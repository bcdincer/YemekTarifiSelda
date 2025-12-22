# AI Servis Test Rehberi

## 🚀 Hızlı Test Adımları

### 1. BackendApi'yi Başlat
```powershell
cd BackendApi
dotnet run
```

BackendApi şu portlarda çalışacak:
- HTTPS: `https://localhost:7016`
- HTTP: `http://localhost:5204`

**Kontrol:** Terminal'de şu mesajı görmelisiniz:
```
Now listening on: https://localhost:7016
Now listening on: http://localhost:5204
```

### 2. FrontendMvc'yi Başlat (Yeni bir terminal)
```powershell
cd FrontendMvc
dotnet run
```

FrontendMvc şu portlarda çalışacak:
- HTTPS: `https://localhost:7036`
- HTTP: `http://localhost:5210`

### 3. Tarif Detay Sayfasına Git
1. Tarayıcıda `https://localhost:7036` adresine git
2. Herhangi bir tarife tıkla (tarif detay sayfasına git)
3. Sayfada **"Malzemeler"** bölümünü bul

### 4. AI Özelliğini Test Et

#### Test Senaryosu 1: AI Kapalı (Matematiksel Hesaplama)
1. **AI toggle'ı KAPALI** bırak (checkbox işaretli değil)
2. **Kişi sayısını** değiştir (örn: 4 → 6)
3. **Beklenen:** Malzemeler matematiksel olarak hesaplanır
   - Örnek: "300 gr" → "450 gr" (300 * 6/4 = 450)

#### Test Senaryosu 2: AI Açık (Yapay Zeka Hesaplama)
1. **AI toggle'ı AÇ** (checkbox'ı işaretle) - "🧠 AI" yazısının yanındaki checkbox
2. **Kişi sayısını** değiştir (örn: 4 → 6)
3. **Beklenen:** 
   - Input birkaç saniye disable olur (loading)
   - Malzemeler AI tarafından hesaplanır
   - Türkçe ifadeler doğru yorumlanır:
     - "yarım su bardağı" → "3/4 su bardağı" veya "0.75 su bardağı"
     - "1 çay bardağı" → "1.5 çay bardağı"
   - Daha doğal dil formatında sonuçlar gelir

### 5. Console'u Kontrol Et
Tarayıcıda **F12** tuşuna bas → **Console** sekmesine git

**Başarılı durumda göreceğiniz:**
```
AI adjustment successful
```

**Hata durumunda göreceğiniz:**
```
AI adjustment error: [hata mesajı]
Error adjusting servings: [hata mesajı]
```

### 6. BackendApi Loglarını Kontrol Et
BackendApi terminal'inde şunları görebilirsiniz:

**Başarılı istek:**
```
[Information] OpenAI API called successfully
[Information] Adjusted ingredients received from AI
```

**Hata durumunda:**
```
[Error] OpenAI API error: [hata detayı]
[Warning] Falling back to mathematical calculation
```

## 🔍 Manuel API Testi (Opsiyonel)

Postman veya curl ile direkt API'yi test edebilirsiniz:

### PowerShell ile Test:
```powershell
$body = @{
    ingredients = @(
        "300 gr petibör bisküvi",
        "100 gr eritilmiş margarin ya da tereyağı",
        "1 çay bardağı ceviz kırığı",
        "1 su bardağı süt (200 ml)",
        "yarım su bardağı toz şeker",
        "3 yemek kaşığı kakao"
    )
    originalServings = 4
    newServings = 6
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:7016/api/recipes/adjust-ingredients" `
    -Method POST `
    -Body $body `
    -ContentType "application/json" `
    -SkipCertificateCheck
```

**Not:** PowerShell 6+ için `-SkipCertificateCheck` kullanılır. Eski versiyonlarda SSL sertifika hatası alabilirsiniz.

## ✅ Başarı Kriterleri

1. ✅ AI toggle açıkken malzemeler güncelleniyor
2. ✅ AI toggle kapalıyken matematiksel hesaplama çalışıyor
3. ✅ "yarım", "çeyrek" gibi Türkçe ifadeler doğru yorumlanıyor
4. ✅ Hata durumunda otomatik olarak matematiksel hesaplamaya geçiyor
5. ✅ Console'da hata mesajı yok
6. ✅ BackendApi loglarında başarılı istek görünüyor

## 🐛 Sorun Giderme

### Problem: AI çalışmıyor, sadece matematiksel hesaplama yapıyor
**Çözüm:**
1. BackendApi'nin çalıştığından emin olun
2. `appsettings.json`'da API key'in doğru olduğunu kontrol edin
3. Console'da hata mesajı var mı bakın
4. BackendApi loglarını kontrol edin

### Problem: "Network error" veya "Failed to fetch"
**Çözüm:**
1. BackendApi'nin `https://localhost:7016` adresinde çalıştığından emin olun
2. CORS ayarlarını kontrol edin
3. SSL sertifika hatası varsa tarayıcıda "Advanced" → "Proceed to localhost" seçin

### Problem: API key hatası
**Çözüm:**
1. `BackendApi/appsettings.json` dosyasında API key'in doğru olduğundan emin olun
2. OpenAI API key'inizin aktif olduğunu kontrol edin
3. API key'in yeterli kredisi olduğundan emin olun

## 📝 Test Örnekleri

### Örnek 1: Basit Miktar Artışı
- **Orijinal:** 4 kişilik, "300 gr un"
- **Yeni:** 6 kişilik
- **Beklenen (AI):** "450 gr un"
- **Beklenen (Matematik):** "450 gr un"

### Örnek 2: Kesirli Miktar
- **Orijinal:** 4 kişilik, "yarım su bardağı süt"
- **Yeni:** 6 kişilik
- **Beklenen (AI):** "3/4 su bardağı süt" veya "0.75 su bardağı süt"
- **Beklenen (Matematik):** "0.75 su bardağı süt"

### Örnek 3: Karmaşık Miktar
- **Orijinal:** 4 kişilik, "1 çay bardağı ceviz kırığı"
- **Yeni:** 6 kişilik
- **Beklenen (AI):** "1.5 çay bardağı ceviz kırığı" veya "1 buçuk çay bardağı ceviz kırığı"
- **Beklenen (Matematik):** "1.5 çay bardağı ceviz kırığı"

