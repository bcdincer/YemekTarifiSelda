# Email Entegrasyonu - Kurulum Rehberi

## ✅ Tamamlanan İşlemler

- ✅ MailKit package eklendi
- ✅ EmailService SMTP implementasyonu tamamlandı
- ✅ Hotmail/Outlook SMTP ayarları yapılandırıldı

## 🔧 Configuration

### appsettings.json

Email ayarları `appsettings.json` dosyasına eklendi:

```json
{
  "Email": {
    "SmtpHost": "smtp-mail.outlook.com",
    "SmtpPort": "587",
    "Username": "burakcandincer89@hotmail.com",
    "Password": "YOUR_PASSWORD_HERE",
    "From": "burakcandincer89@hotmail.com",
    "FromName": "Recipe Site"
  }
}
```

### ⚠️ ÖNEMLİ: Şifre Güvenliği

**Şifrenizi `appsettings.json` dosyasına DOĞRUDAN yazmayın!**

Production ortamında şifrelerinizi güvenli bir şekilde saklamak için:

#### 1. User Secrets (Development için önerilen)

```bash
cd BackendApi
dotnet user-secrets set "Email:Password" "your-actual-password"
```

Bu şekilde şifre `appsettings.json` dosyasında görünmez.

#### 2. Environment Variables (Production için)

```bash
# Windows
set Email__Password=your-actual-password

# Linux/Mac
export Email__Password=your-actual-password
```

#### 3. Azure Key Vault (Production - Önerilen)

Azure'da çalışıyorsanız, Key Vault kullanın.

### 📝 Şifreyi Manuel Olarak Eklemek İsterseniz

`appsettings.json` dosyasında:
```json
"Password": "GERÇEK_ŞİFRENİZ"
```

**Dikkat:** Bu dosyayı Git'e commit etmeyin! `.gitignore`'da olmalı veya `appsettings.local.json` kullanın.

## 🔐 Hotmail/Outlook Özel Notlar

### İki Faktörlü Doğrulama (2FA) Açıksa

Eğer Hotmail hesabınızda 2FA (İki Faktörlü Doğrulama) açıksa, normal şifre yerine **App Password** kullanmanız gerekir:

1. https://account.microsoft.com/security adresine gidin
2. "Advanced security options" bölümüne gidin
3. "App passwords" altında yeni bir app password oluşturun
4. Bu app password'ü `Email:Password` olarak kullanın

### Hotmail Günlük Limitler

Hotmail/Outlook'un günlük email gönderim limitleri:
- **Günde en fazla:** 300 email
- **Her email'de en fazla:** 100 alıcı
- **Saatlik limit:** 30 email

Bu limitleri aşarsanız, hesabınız geçici olarak kısıtlanabilir.

## 🧪 Test Etme

### 1. Tarif Oluşturarak Test

Yeni bir tarif oluşturduğunuzda, admin email adresine otomatik bildirim gönderilir:

```csharp
// RecipeService.CreateAsync içinde
var recipeCreatedEvent = new RecipeCreatedEvent(...);
await _eventPublisher.PublishAsync(recipeCreatedEvent);
// Event handler arka planda email gönderir (Hangfire queue'da)
```

### 2. Manuel Test Endpoint'i (İsteğe Bağlı)

Test için bir endpoint ekleyebilirsiniz:

```csharp
// Program.cs'ye ekleyin (Development only)
if (app.Environment.IsDevelopment())
{
    app.MapPost("/api/test-email", async (IEmailService emailService) =>
    {
        await emailService.SendNotificationEmailAsync(
            "test@example.com",
            "Test Email",
            "<h1>Bu bir test emailidir</h1>",
            isHtml: true
        );
        return Results.Ok(new { message = "Test email sent" });
    });
}
```

## 📧 Email Template'leri

### 1. Verification Email (Email Doğrulama)

Kullanıcı kayıt olduğunda gönderilir.

### 2. Password Reset Email (Şifre Sıfırlama)

Kullanıcı şifre sıfırlama istediğinde gönderilir.

### 3. Recipe Created Notification (Tarif Oluşturuldu Bildirimi)

Yeni tarif eklendiğinde admin'e gönderilir.

## 🔍 Troubleshooting

### Hata: "Authentication failed"

**Çözüm:**
- Şifrenizin doğru olduğundan emin olun
- 2FA açıksa App Password kullanın
- Kullanıcı adının tam email adresi olduğundan emin olun (`burakcandincer89@hotmail.com`)

### Hata: "Connection timeout"

**Çözüm:**
- Firewall'unuzun 587 portunu engellemediğinden emin olun
- SMTP sunucusunun doğru olduğundan emin olun (`smtp-mail.outlook.com`)

### Hata: "The operation has timed out"

**Çözüm:**
- İnternet bağlantınızı kontrol edin
- SMTP port'unun doğru olduğundan emin olun (587)

### Email Gönderilmiyor

**Kontrol Listesi:**
1. ✅ Configuration doğru mu? (`appsettings.json`)
2. ✅ Şifre doğru mu?
3. ✅ SMTP ayarları doğru mu?
4. ✅ Hangfire çalışıyor mu? (Background jobs için)
5. ✅ Log'larda hata var mı?

Log'ları kontrol edin:
```bash
# Uygulama log'larında "Email sent successfully" mesajını arayın
# Hata varsa, detaylı hata mesajı görünecektir
```

## 🚀 Production Önerileri

1. **SendGrid veya AWS SES Kullanın**
   - Daha yüksek güvenilirlik
   - Daha yüksek gönderim limitleri
   - Daha iyi deliverability

2. **Email Queue Priority**
   - Kritik email'ler için özel queue kullanın
   - `BackgroundJob.Enqueue(() => SendEmailAsync(), "emails")`

3. **Retry Logic**
   - Hangfire otomatik retry yapıyor (3 attempts)
   - Gerekirse custom retry logic ekleyin

4. **Monitoring**
   - Email gönderim başarı oranını izleyin
   - Bounce rate'i takip edin
   - Hangfire Dashboard'u kullanın

## 📚 Referanslar

- MailKit Documentation: https://github.com/jstedfast/MailKit
- Outlook SMTP Settings: https://support.microsoft.com/tr-tr/office/outlook-com-için-pop-imap-ve-smtp-ayarları
- Hangfire Documentation: https://www.hangfire.io/

## ✅ Sonraki Adımlar

1. Şifrenizi User Secrets veya Environment Variables'a ekleyin
2. Bir test email gönderin
3. Yeni tarif oluşturup email bildirimi alıp almadığınızı kontrol edin
4. (Opsiyonel) Production için SendGrid veya AWS SES'e geçiş yapın

