# AWS S3 Yapılandırma Kılavuzu

## 1. AWS S3 Bucket Oluşturma

✅ **Bucket zaten oluşturulmuş: `yemektarifleri-bucadi`**

Eğer yeni bir bucket oluşturmanız gerekirse:
1. AWS Console'a giriş yapın
2. S3 servisine gidin
3. "Create bucket" butonuna tıklayın
4. Bucket adını girin: `yemektarifleri-bucadi`
5. Region seçin: `us-east-1` (ABD Doğu - K. Virginia)
6. Public access ayarlarını yapılandırın:
   - "Block all public access" seçeneğini KAPATIN (fotoğrafların erişilebilir olması için)
   - "I acknowledge that the current settings might result in this bucket and the objects within it becoming public" onay kutusunu işaretleyin
7. Bucket'ı oluşturun

## 2. Bucket Policy Ayarlama

Bucket'ınızın "Permissions" sekmesine gidin ve "Bucket policy" bölümüne aşağıdaki policy'yi ekleyin:

**Hazır policy dosyası:** `BackendApi/AWS_S3_BUCKET_POLICY.json`

Veya manuel olarak ekleyin:
```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "PublicReadGetObject",
            "Effect": "Allow",
            "Principal": "*",
            "Action": "s3:GetObject",
            "Resource": "arn:aws:s3:::yemektarifleri-bucadi/*"
        }
    ]
}
```

**Adımlar:**
1. S3 Console'da `yemektarifleri-bucadi` bucket'ına gidin
2. "Permissions" (İzinler) sekmesine tıklayın
3. "Bucket policy" bölümüne gidin
4. "Edit" butonuna tıklayın
5. Yukarıdaki JSON'u yapıştırın
6. "Save changes" butonuna tıklayın

## 3. CORS Ayarlama

Bucket'ınızın "Permissions" sekmesine gidin ve "Cross-origin resource sharing (CORS)" bölümüne aşağıdaki CORS yapılandırmasını ekleyin:

**Hazır CORS dosyası:** `BackendApi/AWS_S3_CORS_CONFIG.json`

Veya manuel olarak ekleyin:
```json
[
    {
        "AllowedHeaders": ["*"],
        "AllowedMethods": ["GET", "PUT", "POST", "DELETE", "HEAD"],
        "AllowedOrigins": ["*"],
        "ExposeHeaders": ["ETag", "x-amz-server-side-encryption", "x-amz-request-id", "x-amz-id-2"],
        "MaxAgeSeconds": 3000
    }
]
```

**Adımlar:**
1. S3 Console'da `yemektarifleri-bucadi` bucket'ına gidin
2. "Permissions" (İzinler) sekmesine tıklayın
3. "Cross-origin resource sharing (CORS)" bölümüne gidin
4. "Edit" butonuna tıklayın
5. Yukarıdaki JSON'u yapıştırın
6. "Save changes" butonuna tıklayın

## 4. IAM Kullanıcı Oluşturma ve Policy Ekleme

**Hazır IAM Policy dosyası:** `BackendApi/AWS_S3_IAM_POLICY.json`

**Adımlar:**

1. **IAM Policy Oluşturma:**
   - IAM Console'a gidin
   - "Policies" > "Create policy" tıklayın
   - "JSON" sekmesine gidin
   - `AWS_S3_IAM_POLICY.json` dosyasındaki içeriği yapıştırın
   - Policy adını girin: `S3RecipeUploadPolicy`
   - "Create policy" butonuna tıklayın

2. **IAM Kullanıcı Oluşturma:**
   - IAM Console'da "Users" > "Create user" tıklayın
   - Kullanıcı adını girin: `s3-recipe-upload-user`
   - "Next" butonuna tıklayın
   - "Attach policies directly" seçeneğini seçin
   - Oluşturduğunuz `S3RecipeUploadPolicy` policy'sini seçin
   - "Next" > "Create user" tıklayın

3. **Access Key Oluşturma:**
   - Oluşturduğunuz kullanıcıya tıklayın
   - "Security credentials" sekmesine gidin
   - "Create access key" butonuna tıklayın
   - "Application running outside AWS" seçeneğini seçin
   - "Next" > "Create access key" tıklayın
   - **Access Key ID** ve **Secret Access Key** değerlerini kopyalayın ve güvenli bir yerde saklayın
   - ⚠️ **ÖNEMLİ:** Secret Access Key sadece bir kez gösterilir, kaydedin!

## 5. appsettings.json Yapılandırması

✅ **appsettings.json zaten güncellenmiş!**

Sadece AccessKey ve SecretKey değerlerini eklemeniz gerekiyor:

```json
{
  "AWS": {
    "AccessKey": "YOUR_ACCESS_KEY_HERE",  // ← IAM kullanıcısından aldığınız Access Key ID
    "SecretKey": "YOUR_SECRET_KEY_HERE",  // ← IAM kullanıcısından aldığınız Secret Access Key
    "Region": "us-east-1",
    "S3": {
      "BucketName": "yemektarifleri-bucadi",
      "UseBaseUrl": true,
      "BaseUrl": "yemektarifleri-bucadi.s3.us-east-1.amazonaws.com"
    }
  }
}
```

### Güvenlik Notu:
- **Production'da** AccessKey ve SecretKey'i `appsettings.json` yerine:
  - Environment Variables kullanın
  - Azure Key Vault kullanın
  - AWS Secrets Manager kullanın
  - User Secrets kullanın (development için)

### User Secrets Kullanımı (Development - Önerilen):

Güvenlik için AccessKey ve SecretKey'i appsettings.json yerine User Secrets'da saklayın:

```bash
cd BackendApi
dotnet user-secrets set "AWS:AccessKey" "YOUR_ACCESS_KEY_ID"
dotnet user-secrets set "AWS:SecretKey" "YOUR_SECRET_ACCESS_KEY"
dotnet user-secrets set "AWS:Region" "us-east-1"
dotnet user-secrets set "AWS:S3:BucketName" "yemektarifleri-bucadi"
dotnet user-secrets set "AWS:S3:UseBaseUrl" "true"
dotnet user-secrets set "AWS:S3:BaseUrl" "yemektarifleri-bucadi.s3.us-east-1.amazonaws.com"
```

**Not:** User Secrets kullanırsanız, appsettings.json'daki AccessKey ve SecretKey alanlarını boş bırakabilirsiniz.

## 6. Dosya Yapısı

S3'te dosyalar şu şekilde saklanacak:
- **Key (Anahtar):** `uploads/recipes/{GUID}_{filename}.jpg`
- **Full URL:** `https://yemektarifleri-bucadi.s3.us-east-1.amazonaws.com/uploads/recipes/{GUID}_{filename}.jpg`

**Örnek:**
- Key: `uploads/recipes/123e4567-e89b-12d3-a456-426614174000_hamburger.png`
- URL: `https://yemektarifleri-bucadi.s3.us-east-1.amazonaws.com/uploads/recipes/123e4567-e89b-12d3-a456-426614174000_hamburger.png`

## 7. Güvenlik Özellikleri

- ✅ Dosya tipi kontrolü (sadece JPG, PNG, GIF, WebP)
- ✅ Dosya boyutu kontrolü (maksimum 5MB)
- ✅ MIME type kontrolü
- ✅ Dosya adı sanitization (tehlikeli karakterler temizlenir)
- ✅ Benzersiz dosya adları (GUID kullanılır)
- ✅ Authentication gerektirir (sadece giriş yapmış kullanıcılar yükleyebilir)

## 8. Test Etme

1. Uygulamayı çalıştırın
2. Bir tarif oluştururken fotoğraf yükleyin
3. Fotoğrafın S3'te göründüğünü kontrol edin
4. Fotoğraf URL'sinin erişilebilir olduğunu kontrol edin

