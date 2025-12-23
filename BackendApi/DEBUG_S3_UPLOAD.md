# S3 Yükleme Sorun Giderme Kılavuzu

## Sorun: Fotoğraflar S3'e yüklenmiyor veya görünmüyor

### 1. Access Key Kontrolü

User Secrets'ta access key'lerin kayıtlı olduğundan emin olun:

```bash
cd BackendApi
dotnet user-secrets list
```

Şunları görmelisiniz:
- `AWS:AccessKey`
- `AWS:SecretKey`

Eğer yoksa:
```bash
dotnet user-secrets set "AWS:AccessKey" "YOUR_ACCESS_KEY_ID"
dotnet user-secrets set "AWS:SecretKey" "YOUR_SECRET_ACCESS_KEY"
```

### 2. Backend Loglarını Kontrol Edin

Uygulamayı çalıştırın ve tarif oluştururken fotoğraf yüklemeyi deneyin. Backend console'da şu logları arayın:

- ✅ Başarılı: `"File uploaded to S3 by user {UserId}: {Url}"`
- ❌ Hata: `"Error uploading file to S3"` veya `"S3 error uploading file"`

### 3. S3 Test Endpoint'ini Kullanın

Tarayıcıda şu URL'yi açın (giriş yapmış olmalısınız):
```
GET https://localhost:7036/api/s3/test
```

Başarılı yanıt:
```json
{
  "message": "S3 service is configured correctly",
  "bucketUrl": "https://yemektarifleri-bucadi.s3.us-east-1.amazonaws.com/test/connection.txt",
  "timestamp": "..."
}
```

### 4. Upload Endpoint'ini Test Edin

Postman veya curl ile test edin:

```bash
curl -X POST https://localhost:7036/api/upload/image \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -F "file=@test-image.jpg"
```

### 5. S3 Console'da Kontrol Edin

1. AWS S3 Console'a gidin
2. `yemektarifleri-bucadi` bucket'ını açın
3. `uploads/recipes/` klasörüne bakın
4. Yeni yüklenen dosyaları görmelisiniz

### 6. Yaygın Hatalar ve Çözümleri

#### Hata: "AWS AccessKey and SecretKey are required"
- **Çözüm:** User Secrets'ta access key'leri kontrol edin

#### Hata: "Access Denied" veya "403 Forbidden"
- **Çözüm:** IAM kullanıcısının S3'e yazma izni olduğundan emin olun
- IAM Policy'yi kontrol edin: `AWS_S3_IAM_POLICY.json`

#### Hata: "Bucket not found"
- **Çözüm:** Bucket adının doğru olduğundan emin olun: `yemektarifleri-bucadi`
- Region'ın doğru olduğundan emin olun: `us-east-1`

#### Fotoğraflar yükleniyor ama görünmüyor
- **Çözüm:** 
  1. S3 Console'da dosyanın "Public" olduğundan emin olun
  2. Bucket Policy'nin doğru yapılandırıldığından emin olun
  3. URL formatını kontrol edin: `https://yemektarifleri-bucadi.s3.us-east-1.amazonaws.com/uploads/recipes/...`

### 7. Frontend'de Debug

Tarayıcı console'unda (F12) şu hataları kontrol edin:
- Network sekmesinde `/api/upload/image` isteğini kontrol edin
- Response'u kontrol edin
- Hata mesajlarını okuyun

### 8. Veritabanında Kontrol

Yeni eklenen tarifin Images'larını kontrol edin:

```sql
SELECT r.Id, r.Title, ri.ImageUrl, ri.IsPrimary, ri.DisplayOrder
FROM "Recipes" r
LEFT JOIN "RecipeImages" ri ON r.Id = ri."RecipeId"
WHERE r.Id = YOUR_RECIPE_ID
ORDER BY ri."DisplayOrder";
```

Eğer Images yoksa, yükleme başarısız olmuş demektir.
