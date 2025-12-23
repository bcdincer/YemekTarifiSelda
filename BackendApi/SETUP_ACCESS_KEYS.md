# Access Key'leri Yapılandırma

## AWS Console'dan Access Key Oluşturma

1. AWS Console'a gidin: https://console.aws.amazon.com/
2. IAM servisine gidin
3. "Users" bölümüne gidin
4. S3 erişimi olan kullanıcıyı seçin
5. "Security credentials" sekmesine gidin
6. "Create access key" butonuna tıklayın
7. "Application running outside AWS" seçeneğini seçin
8. Yeni Access Key ID ve Secret Access Key'i kopyalayın
9. **ÖNEMLİ:** Secret Access Key'i sadece bir kez görebilirsiniz!

## Güvenli Yapılandırma (User Secrets - Önerilen)

Terminal'de şu komutları çalıştırın (YOUR_ACCESS_KEY_ID ve YOUR_SECRET_ACCESS_KEY yerine gerçek değerleri yazın):

```bash
cd BackendApi
dotnet user-secrets set "AWS:AccessKey" "YOUR_ACCESS_KEY_ID"
dotnet user-secrets set "AWS:SecretKey" "YOUR_SECRET_ACCESS_KEY"
```

## Alternatif: appsettings.json (Sadece Development için - ÖNERİLMEZ)

⚠️ **UYARI:** Production'da asla appsettings.json'a access key yazmayın!

Eğer user secrets kullanmak istemiyorsanız (sadece development için):
```json
{
  "AWS": {
    "AccessKey": "YOUR_ACCESS_KEY_ID",
    "SecretKey": "YOUR_SECRET_ACCESS_KEY",
    ...
  }
}
```
