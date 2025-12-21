# Email Şifresi Ekleme - Adım Adım

## ✅ User Secrets Hazır

User Secrets zaten initialize edildi. Şimdi şifrenizi ekleyin.

## 📝 Şifrenizi Eklemek İçin

Aşağıdaki komutu çalıştırın ve `YOUR_PASSWORD_HERE` yerine **gerçek şifrenizi** yazın:

```bash
cd C:\Users\burak.dincer\Desktop\YeniDotnetProje\BackendApi
dotnet user-secrets set "Email:Password" "YOUR_PASSWORD_HERE"
```

### Örnek:

Eğer şifreniz `MySecretPassword123` ise:

```bash
dotnet user-secrets set "Email:Password" "MySecretPassword123"
```

## ⚠️ ÖNEMLİ: 2FA (İki Faktörlü Doğrulama) Açıksa

Eğer Hotmail hesabınızda **İki Faktörlü Doğrulama (2FA)** açıksa:

1. Normal şifreniz çalışmayacaktır
2. **App Password** oluşturmanız gerekir:

   **Adımlar:**
   1. Tarayıcınızda https://account.microsoft.com/security adresine gidin
   2. Oturum açın
   3. "Advanced security options" (Gelişmiş güvenlik seçenekleri) bölümüne gidin
   4. "App passwords" (Uygulama şifreleri) bölümünü bulun
   5. "Create a new app password" (Yeni uygulama şifresi oluştur) butonuna tıklayın
   6. Bir açıklama girin (örn: "Recipe Site Email Service")
   7. Oluşturulan şifreyi kopyalayın
   8. Bu şifreyi yukarıdaki komutta kullanın

## ✅ Şifreyi Kontrol Etme

Şifrenizin eklendiğini kontrol etmek için:

```bash
dotnet user-secrets list
```

Çıktıda şunu görmelisiniz:
```
Email:Password = YourPasswordHere
```

## 🔒 Güvenlik

- ✅ User Secrets şifrenizi `appsettings.json` dosyasından ayrı tutar
- ✅ Git'e commit edilmez (otomatik olarak `.gitignore`'da)
- ✅ Sadece sizin bilgisayarınızda saklanır
- ✅ Development ortamı için güvenlidir

## 🧪 Test Etme

Şifrenizi ekledikten sonra:

1. Backend API'yi çalıştırın
2. Yeni bir tarif oluşturun
3. Log'larda "Email sent successfully" mesajını görmelisiniz
4. Admin email adresine (`admin@example.com` - şu an) bildirim email'i gitmeli

## ❓ Hata Alırsanız

**"Authentication failed" hatası:**
- Şifrenizin doğru olduğundan emin olun
- 2FA açıksa App Password kullanın
- Email adresinin tam olduğundan emin olun (`burakcandincer89@hotmail.com`)

**"Connection timeout" hatası:**
- İnternet bağlantınızı kontrol edin
- Firewall'unuzun 587 portunu engellemediğinden emin olun

## 📋 Özet

1. Terminal/PowerShell'i açın
2. BackendApi klasörüne gidin
3. Komutu çalıştırın (şifrenizi yazın):
   ```bash
   dotnet user-secrets set "Email:Password" "SİZİN_ŞİFRENİZ"
   ```
4. Kontrol edin:
   ```bash
   dotnet user-secrets list
   ```
5. Uygulamayı çalıştırın ve test edin

Hazırsınız! 🚀

