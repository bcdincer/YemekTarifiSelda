using Amazon.S3;
using Amazon.S3.Model;
using BackendApi.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BackendApi.Infrastructure.Services;

public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _region;
    private readonly string _baseUrl;
    private readonly ILogger<S3Service> _logger;

    public S3Service(
        IConfiguration configuration,
        ILogger<S3Service> logger)
    {
        _logger = logger;
        
        // AWS ayarlarını al
        var accessKey = configuration["AWS:AccessKey"];
        var secretKey = configuration["AWS:SecretKey"];
        _region = configuration["AWS:Region"] ?? "us-east-1";
        _bucketName = configuration["AWS:S3:BucketName"] ?? throw new InvalidOperationException("AWS S3 BucketName is required");
        var useBaseUrl = configuration.GetValue<bool>("AWS:S3:UseBaseUrl", false);
        _baseUrl = configuration["AWS:S3:BaseUrl"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
        {
            _logger.LogError("AWS AccessKey or SecretKey is missing. AccessKey: {HasAccessKey}, SecretKey: {HasSecretKey}", 
                !string.IsNullOrWhiteSpace(accessKey), !string.IsNullOrWhiteSpace(secretKey));
            throw new InvalidOperationException("AWS AccessKey and SecretKey are required. Please check your user secrets or appsettings.json");
        }

        // Access Key'in formatını kontrol et (AWS Access Key ID genellikle "AKIA" ile başlar)
        if (!string.IsNullOrWhiteSpace(accessKey) && !accessKey.StartsWith("AKIA") && accessKey.Length < 16)
        {
            _logger.LogWarning("AWS AccessKey format looks incorrect. AccessKey should start with 'AKIA' and be at least 16 characters long.");
        }

        var accessKeyPrefix = !string.IsNullOrWhiteSpace(accessKey) && accessKey.Length >= 8 
            ? accessKey.Substring(0, 8) 
            : "***";
        _logger.LogInformation("S3Service initialized. Bucket: {BucketName}, Region: {Region}, BaseUrl: {BaseUrl}, AccessKey: {AccessKeyPrefix}...", 
            _bucketName, _region, _baseUrl, accessKeyPrefix);

        // S3 client oluştur
        var config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_region)
        };

        _s3Client = new AmazonS3Client(accessKey, secretKey, config);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            // Güvenlik: Dosya adını temizle ve benzersiz yap
            var sanitizedFileName = SanitizeFileName(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}_{sanitizedFileName}";
            var s3Key = $"uploads/recipes/{uniqueFileName}";

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = s3Key,
                InputStream = fileStream,
                ContentType = contentType,
                // ACL kullanmıyoruz - bucket policy ile public read erişimi sağlanmalı
                // CannedACL = S3CannedACL.PublicRead, // Bucket ACL'lere izin vermiyor
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256 // Güvenlik için şifreleme
            };

            await _s3Client.PutObjectAsync(request, cancellationToken);

            var fileUrl = GetFileUrl(s3Key);
            _logger.LogInformation("File uploaded to S3: {S3Key}, URL: {FileUrl}", s3Key, fileUrl);

            // URL döndür
            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to S3: {FileName}", fileName);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            // URL'den key'i çıkar
            var s3Key = ExtractKeyFromUrl(fileName);
            if (string.IsNullOrWhiteSpace(s3Key))
            {
                return false;
            }

            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = s3Key
            };

            await _s3Client.DeleteObjectAsync(request, cancellationToken);

            _logger.LogInformation("File deleted from S3: {S3Key}", s3Key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from S3: {FileName}", fileName);
            return false;
        }
    }

    public async Task<bool> FileExistsAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var s3Key = ExtractKeyFromUrl(fileName);
            if (string.IsNullOrWhiteSpace(s3Key))
            {
                return false;
            }

            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = s3Key
            };

            await _s3Client.GetObjectMetadataAsync(request, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence in S3: {FileName}", fileName);
            return false;
        }
    }

    public string GetFileUrl(string fileName)
    {
        // Eğer BaseUrl varsa onu kullan
        if (!string.IsNullOrWhiteSpace(_baseUrl))
        {
            // BaseUrl zaten tam URL olabilir veya sadece domain olabilir
            if (_baseUrl.StartsWith("http"))
            {
                return $"{_baseUrl.TrimEnd('/')}/{fileName}";
            }
            return $"https://{_baseUrl.TrimEnd('/')}/{fileName}";
        }

        // BaseUrl yoksa standart S3 URL formatını kullan
        return $"https://{_bucketName}.s3.{_region}.amazonaws.com/{fileName}";
    }

    private string SanitizeFileName(string fileName)
    {
        // Güvenlik: Dosya adındaki tehlikeli karakterleri temizle
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        
        // Uzunluk kontrolü
        if (sanitized.Length > 100)
        {
            var extension = Path.GetExtension(sanitized);
            sanitized = sanitized.Substring(0, 100 - extension.Length) + extension;
        }

        return sanitized;
    }

    private string ExtractKeyFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        // BaseUrl kullanılıyorsa
        if (!string.IsNullOrWhiteSpace(_baseUrl))
        {
            var baseUrl = _baseUrl.StartsWith("http") ? _baseUrl : $"https://{_baseUrl}";
            if (url.StartsWith(baseUrl))
            {
                var key = url.Replace(baseUrl.TrimEnd('/') + "/", "");
                return key;
            }
        }

        // Standart S3 URL formatı: https://bucket.s3.region.amazonaws.com/key
        var s3Pattern = $"{_bucketName}.s3.{_region}.amazonaws.com/";
        if (url.Contains(s3Pattern))
        {
            return url.Substring(url.IndexOf(s3Pattern) + s3Pattern.Length);
        }

        // Eğer zaten key formatındaysa (uploads/recipes/...)
        if (url.StartsWith("uploads/"))
        {
            return url;
        }

        return string.Empty;
    }
}

