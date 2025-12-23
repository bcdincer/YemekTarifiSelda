# CORS Ayarlarını Yapılandırma - Adım Adım Kılavuz

## CORS Nedir?
CORS (Cross-Origin Resource Sharing), farklı domain'lerden kaynaklara erişim için gerekli bir güvenlik mekanizmasıdır. S3 bucket'ınızın CORS ayarları, frontend uygulamanızdan S3'e erişim için gereklidir.

## CORS Ayarlarını Yapma Adımları

### 1. AWS S3 Console'a Giriş Yapın
- AWS Console'a giriş yapın: https://console.aws.amazon.com/s3/
- `yemektarifleri-bucadi` bucket'ını bulun ve tıklayın

### 2. Permissions (İzinler) Sekmesine Gidin
- Bucket detay sayfasında üstteki sekmelerden **"Permissions"** (İzinler) sekmesine tıklayın

### 3. CORS Bölümünü Bulun
- Sayfayı aşağı kaydırın
- **"Cross-origin resource sharing (CORS)"** bölümünü bulun
- Bu bölümde ya boş bir alan ya da mevcut bir CORS yapılandırması görünecek

### 4. CORS Yapılandırmasını Düzenleyin
- **"Edit"** (Düzenle) butonuna tıklayın
- Aşağıdaki JSON'u kopyalayıp yapıştırın:

```json
[
    {
        "AllowedHeaders": [
            "*"
        ],
        "AllowedMethods": [
            "GET",
            "PUT",
            "POST",
            "DELETE",
            "HEAD"
        ],
        "AllowedOrigins": [
            "*"
        ],
        "ExposeHeaders": [
            "ETag",
            "x-amz-server-side-encryption",
            "x-amz-request-id",
            "x-amz-id-2"
        ],
        "MaxAgeSeconds": 3000
    }
]
```

### 5. Kaydedin
- **"Save changes"** (Değişiklikleri kaydet) butonuna tıklayın
- Başarılı bir şekilde kaydedildiğine dair bir onay mesajı göreceksiniz

## CORS Yapılandırmasını Kontrol Etme

CORS ayarlarınızın doğru yapılandırıldığını kontrol etmek için:

1. S3 Console'da bucket'ınıza gidin
2. Permissions sekmesine gidin
3. CORS bölümünde yukarıdaki JSON'u görmelisiniz

## Sorun Giderme

### CORS Hatası Alıyorsanız:
- CORS yapılandırmasının kaydedildiğinden emin olun
- Tarayıcı cache'ini temizleyin
- CORS yapılandırmasında `AllowedOrigins` değerinin `["*"]` olduğundan emin olun (development için)

### Production için Daha Güvenli CORS:
Production ortamında, `AllowedOrigins` değerini sadece kendi domain'inizle sınırlayın:

```json
"AllowedOrigins": [
    "https://yourdomain.com",
    "https://www.yourdomain.com"
]
```

## Hazır CORS Dosyası
`BackendApi/AWS_S3_CORS_CONFIG.json` dosyasında hazır CORS yapılandırması bulunmaktadır.

