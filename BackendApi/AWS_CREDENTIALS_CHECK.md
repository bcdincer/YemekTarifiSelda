# AWS Credentials Kontrol Kılavuzu

## Hata Mesajı
```
S3 error: The AWS Access Key Id you provided does not exist in our records.
```

Bu hata, AWS Access Key ID'nin geçersiz, silinmiş veya yanlış olduğunu gösterir.

## Çözüm Adımları

### 1. AWS Console'da IAM Kullanıcısını Kontrol Edin

1. AWS Console'a gidin: https://console.aws.amazon.com/
2. IAM servisine gidin
3. "Users" bölümüne gidin
4. S3 erişimi olan kullanıcıyı bulun
5. "Security credentials" sekmesine gidin
6. "Access keys" bölümünde aktif key'leri kontrol edin

### 2. Yeni Access Key Oluşturun (Gerekirse)

Eğer mevcut key geçersizse:

1. IAM Console'da kullanıcıyı seçin
2. "Security credentials" sekmesine gidin
3. "Create access key" butonuna tıklayın
4. "Application running outside AWS" seçeneğini seçin
5. Yeni Access Key ID ve Secret Access Key'i kopyalayın
6. **ÖNEMLİ:** Secret Access Key'i sadece bir kez görebilirsiniz!

### 3. User Secrets'a Yeni Key'leri Ekleyin

```bash
cd BackendApi
dotnet user-secrets set "AWS:AccessKey" "YOUR_ACCESS_KEY_ID"
dotnet user-secrets set "AWS:SecretKey" "YOUR_SECRET_ACCESS_KEY"
```

### 4. IAM Policy'yi Kontrol Edin

Kullanıcının şu policy'lere sahip olduğundan emin olun:

- `AmazonS3FullAccess` (veya daha kısıtlı bir policy)
- Bucket'a özel erişim izinleri

Policy dosyası: `BackendApi/AWS_S3_IAM_POLICY.json`

### 5. Backend API'yi Yeniden Başlatın

User secrets değişikliklerinin uygulanması için backend API'yi yeniden başlatın.

### 6. Test Edin

Backend console'unda şu logları kontrol edin:
- `"S3Service initialized. Bucket: yemektarifleri-bucadi, Region: us-east-1"`
- `"File uploaded to S3 by user {UserId}: {Url}"`

## Mevcut User Secrets

User Secrets'ta kayıtlı credentials'ları kontrol etmek için:
```bash
cd BackendApi
dotnet user-secrets list
```

Eğer key'ler geçersizse, AWS Console'dan yeni key'ler oluşturun ve yukarıdaki adımları takip edin.
