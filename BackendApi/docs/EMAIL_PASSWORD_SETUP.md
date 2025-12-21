# Email Şifresi Ekleme - Hızlı Başlangıç

## 🔐 Şifrenizi Ekleyin

### Yöntem 1: User Secrets (ÖNERİLEN - Development için)

User Secrets kullanarak şifrenizi güvenli bir şekilde saklayın:

```bash
cd BackendApi
dotnet user-secrets set "Email:Password" "SİZİN_ŞİFRENİZ"
```

**Örnek:**
```bash
dotnet user-secrets set "Email:Password" "MyPassword123"
```

Bu şekilde şifre `appsettings.json` dosyasında görünmez ve Git'e commit edilmez.

### Yöntem 2: appsettings.json'a Direkt Ekleme (SADECE TEST İÇİN)

⚠️ **UYARI:** Bu yöntem sadece test için kullanın, şifrenizi Git'e commit etmeyin!

`appsettings.json` dosyasında şu satırı bulun:
```json
"Password": "YOUR_PASSWORD_HERE",
```

Şunu yapın:
```json
"Password": "GERÇEK_ŞİFRENİZ",
```

## ⚠️ ÖNEMLİ NOT: 2FA (İki Faktörlü Doğrulama)

Eğer Hotmail hesabınızda **İki Faktörlü Doğrulama (2FA)** açıksa:

1. Normal şifreniz çalışmayacaktır
2. **App Password** oluşturmanız gerekir:
   - https://account.microsoft.com/security adresine gidin
   - "Advanced security options" → "App passwords"
   - Yeni bir app password oluşturun
   - Bu app password'ü `Email:Password` olarak kullanın

## ✅ Test Etme

Şifrenizi ekledikten sonra:

1. Uygulamayı çalıştırın
2. Yeni bir tarif oluşturun
3. Admin email adresine (`admin@example.com` - şu an) bildirim email'i gitmeli
4. Log'ları kontrol edin: "Email sent successfully" mesajını görmelisiniz

## 🔍 Hata Alırsanız

**"Authentication failed" hatası:**
- Şifrenizin doğru olduğundan emin olun
- 2FA açıksa App Password kullanın
- Kullanıcı adının tam email adresi olduğundan emin olun

**"Connection timeout" hatası:**
- Firewall'unuzun 587 portunu engellemediğinden emin olun
- İnternet bağlantınızı kontrol edin

## 📝 Detaylı Bilgi

Detaylı kurulum rehberi için `EMAIL_SETUP_GUIDE.md` dosyasına bakın.

