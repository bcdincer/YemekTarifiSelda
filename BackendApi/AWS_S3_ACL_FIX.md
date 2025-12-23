# S3 ACL Hatası Çözümü

## Hata Mesajı
```
S3 error: The bucket does not allow ACLs
```

## Sorun
Modern AWS S3 bucket'ları genellikle ACL (Access Control Lists) kullanımını devre dışı bırakır. Bunun yerine bucket policy kullanılır.

## Çözüm
Kodda `CannedACL = S3CannedACL.PublicRead` kullanımı kaldırıldı. Bunun yerine bucket policy ile public read erişimi sağlanıyor.

## Bucket Policy Kontrolü

Bucket policy'nin doğru yapılandırıldığından emin olun:

1. AWS S3 Console'a gidin
2. `yemektarifleri-bucadi` bucket'ına gidin
3. "Permissions" (İzinler) sekmesine tıklayın
4. "Bucket policy" bölümünü kontrol edin

Bucket policy şu şekilde olmalı:
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

## Object Ownership Ayarları

Eğer hala sorun yaşıyorsanız, "Object Ownership" ayarlarını kontrol edin:

1. S3 Console'da bucket'ın "Permissions" sekmesine gidin
2. "Object Ownership" bölümüne gidin
3. "ACLs disabled (recommended)" seçeneğinin seçili olduğundan emin olun
4. Veya "Bucket owner enforced" seçeneğini seçin

## Test

Backend API'yi yeniden başlattıktan sonra:
1. Yeni bir tarif oluşturun
2. Fotoğraf yüklemeyi deneyin
3. Backend console'unda `"File uploaded to S3 by user {UserId}: {Url}"` logunu kontrol edin
4. Yüklenen fotoğrafın URL'sine tarayıcıdan erişebildiğinizi doğrulayın

