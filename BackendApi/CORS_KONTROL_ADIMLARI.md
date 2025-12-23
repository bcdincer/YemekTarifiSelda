# CORS Ayarlarını Kontrol Etme ve Yapılandırma

## CORS Yapılandırmasını Kontrol Etme

### Yöntem 1: AWS Console'dan Kontrol

1. **AWS S3 Console'a gidin:** https://console.aws.amazon.com/s3/
2. **Bucket'ınızı açın:** `yemektarifleri-bucadi`
3. **Permissions (İzinler) sekmesine tıklayın**
4. **"Cross-origin resource sharing (CORS)" bölümüne gidin**
5. **CORS yapılandırması var mı kontrol edin:**
   - Eğer bir JSON yapılandırması görüyorsanız → ✅ CORS yapılandırılmış
   - Eğer "No CORS configuration" veya boş bir alan görüyorsanız → ❌ CORS yapılandırılmamış

### Yöntem 2: Test Endpoint ile Kontrol

Uygulamayı çalıştırdıktan sonra:
```
GET https://localhost:7036/api/s3/test
```

Bu endpoint S3 bağlantısını test eder.

## CORS Yapılandırmasını Ekleme (Eğer Yoksa)

### Adım 1: AWS S3 Console'a Gidin
- https://console.aws.amazon.com/s3/
- `yemektarifleri-bucadi` bucket'ını seçin

### Adım 2: Permissions Sekmesine Gidin
- Bucket detay sayfasında üstte **"Permissions"** (İzinler) sekmesine tıklayın

### Adım 3: CORS Bölümünü Bulun
- Sayfayı aşağı kaydırın
- **"Cross-origin resource sharing (CORS)"** başlığını bulun
- **"Edit"** (Düzenle) butonuna tıklayın

### Adım 4: CORS JSON'unu Yapıştırın
Aşağıdaki JSON'u kopyalayıp yapıştırın:

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

**Veya hazır dosyayı kullanın:**
- `BackendApi/AWS_S3_CORS_CONFIG.json` dosyasını açın
- İçeriğini kopyalayıp AWS Console'a yapıştırın

### Adım 5: Kaydedin
- **"Save changes"** (Değişiklikleri kaydet) butonuna tıklayın
- Başarı mesajını görmelisiniz

## CORS Yapılandırmasının Doğru Olduğunu Doğrulama

CORS yapılandırması başarıyla eklendikten sonra:

1. AWS Console'da CORS bölümünde JSON'u görmelisiniz
2. Uygulamayı çalıştırıp fotoğraf yüklemeyi deneyin
3. Tarayıcı console'unda CORS hatası olmamalı

## Sorun Giderme

### "CORS policy" hatası alıyorsanız:
1. CORS yapılandırmasının kaydedildiğinden emin olun
2. Tarayıcı cache'ini temizleyin (Ctrl+Shift+Delete)
3. Uygulamayı yeniden başlatın
4. CORS JSON'unun doğru formatta olduğundan emin olun (virgül, tırnak işaretleri kontrol edin)

### CORS yapılandırması görünmüyorsa:
- Bucket'ın doğru olduğundan emin olun (`yemektarifleri-bucadi`)
- Permissions sekmesinde olduğunuzdan emin olun
- Sayfayı yenileyin (F5)

## Özet

✅ **Access Key'ler:** User Secrets'a kaydedildi
⏳ **CORS:** AWS Console'da yapılandırmanız gerekiyor (yukarıdaki adımları takip edin)

CORS yapılandırması yapıldıktan sonra sistem tamamen hazır olacak!

