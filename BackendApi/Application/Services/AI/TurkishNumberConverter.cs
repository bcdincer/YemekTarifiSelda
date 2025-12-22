namespace BackendApi.Application.Services.AI;

/// <summary>
/// Sayıları Türkçe doğal ifadelere çeviren yardımcı sınıf
/// </summary>
public static class TurkishNumberConverter
{
    public static string ToNaturalExpression(double number)
    {
        // Tam sayılar için ondalık kullanma
        if (Math.Abs(number % 1) < 0.01)
        {
            return ((int)number).ToString();
        }
        
        // Özel durumlar - doğal Türkçe ifadeler
        if (Math.Abs(number - 0.5) < 0.01)
        {
            return "yarım";
        }
        if (Math.Abs(number - 0.25) < 0.01)
        {
            return "çeyrek";
        }
        if (Math.Abs(number - 0.75) < 0.01)
        {
            return "üç çeyrek";
        }
        if (Math.Abs(number - 1.5) < 0.01)
        {
            return "bir buçuk";
        }
        if (Math.Abs(number - 2.5) < 0.01)
        {
            return "iki buçuk";
        }
        if (Math.Abs(number - 3.5) < 0.01)
        {
            return "üç buçuk";
        }
        
        // Diğer durumlar için ondalık göster (virgül ile)
        if (number < 1)
        {
            return number.ToString("F2", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).TrimEnd('0').TrimEnd(',');
        }
        else
        {
            return number.ToString("F1", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")).TrimEnd('0').TrimEnd(',');
        }
    }
}


