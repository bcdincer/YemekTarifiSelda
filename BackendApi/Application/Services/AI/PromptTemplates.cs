namespace BackendApi.Application.Services.AI;

/// <summary>
/// AI prompt şablonları için sabitler
/// </summary>
public static class PromptTemplates
{
    public const string SystemMessage = @"Sen bir yemek tarifi asistanısın. Malzeme miktarlarını orantılı olarak hesaplarsın ve sonuçları Türkçe doğal ifadelere çevirirsin.

KRİTİK KURALLAR:
1. 'yarım' = 0.5 olarak hesapla, sonra sonucu doğal ifadeye çevir
2. Hesaplama sonucu 1.5 ise → 'bir buçuk' yaz, ASLA '1,15' veya '1.15' yazma
3. Hesaplama sonucu 0.75 ise → 'üç çeyrek' veya '0.75' yaz
4. Hesaplama sonucu 0.5 ise → 'yarım' yaz
5. Tam sayılar için ondalık kullanma (3.0 → 3, 4.0 → 4)
6. Aralıklı sayıları çarp (1-2 → 3-6 gibi)
7. Sadece güncellenmiş malzeme listesini döndür, açıklama ekleme.";

    public static string BuildIngredientScalingPrompt(
        int originalServings,
        int newServings,
        List<string> ingredients,
        Func<double, string> numberToTurkish)
    {
        var multiplier = (double)newServings / originalServings;

        return $@"Aşağıda {originalServings} kişilik bir tarifin malzeme listesi var.
Kullanıcı kişi sayısını {newServings} olarak değiştirdi.
Çarpan: {multiplier:F2} ({newServings} ÷ {originalServings} = {multiplier:F2})

GÖREV: Her malzemenin miktarını {multiplier:F2} ile çarp ve {newServings} kişilik için güncelle.

KRİTİK KURALLAR:

1. SAYISAL DEĞERLERİ ÇARP:
   - 400 gram → {400 * multiplier:F0} gram (400 × {multiplier:F2} = {400 * multiplier:F0})
   - 8 yemek kaşığı → {8 * multiplier:F0} yemek kaşığı (8 × {multiplier:F2} = {8 * multiplier:F0})
   - 1 adet → {1 * multiplier:F0} adet (1 × {multiplier:F2} = {1 * multiplier:F0})

2. ARALIKLI SAYILARI ÇARP (örn: ""1-2""):
   - 1-2 diş → {1 * multiplier:F0}-{2 * multiplier:F0} diş (her sayıyı ayrı çarp)
   - 3-4 yemek kaşığı → {3 * multiplier:F0}-{4 * multiplier:F0} yemek kaşığı

3. TÜRKÇE İFADELERİ SAYIYA ÇEVİR, ÇARP, SONRA DOĞAL İFADEYE ÇEVİR:
   - ""yarım"" = 0.5 → 0.5 × {multiplier:F2} = {0.5 * multiplier:F2} → ""{numberToTurkish(0.5 * multiplier)}""
   - ""bir buçuk"" = 1.5 → 1.5 × {multiplier:F2} = {1.5 * multiplier:F2} → ""{numberToTurkish(1.5 * multiplier)}""

4. SONUÇ FORMATI:
   - Tam sayılar için ondalık kullanma (3.0 → 3, 6.0 → 6)
   - 1.5 → ""bir buçuk""
   - 0.5 → ""yarım""
   - 0.75 → ""üç çeyrek""
   - ASLA ""1,15"" veya ""1.15"" yazma! 1.5 = ""bir buçuk""

ÖRNEK HESAPLAMA ({originalServings} kişi → {newServings} kişi):
- 400 gram kıyma → {400 * multiplier:F0} gram kıyma
- 8 yemek kaşığı galeta unu → {8 * multiplier:F0} yemek kaşığı galeta unu
- 1 adet yumurta → {1 * multiplier:F0} adet yumurta
- 1-2 diş sarımsak → {1 * multiplier:F0}-{2 * multiplier:F0} diş sarımsak
- Yarım tatlı kaşığı tuz → {numberToTurkish(0.5 * multiplier)} tatlı kaşığı tuz
- 2 yemek kaşığı sıvı yağ → {2 * multiplier:F0} yemek kaşığı sıvı yağ

SADECE güncellenmiş malzeme listesini yaz, başka bir şey ekleme:

Malzemeler:
{string.Join("\n", ingredients.Select((ing, idx) => $"- {ing}"))}

Güncellenmiş malzemeler ({newServings} kişilik):";
    }
}


