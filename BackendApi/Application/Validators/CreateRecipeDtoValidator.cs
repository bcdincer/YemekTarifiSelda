using BackendApi.Application.DTOs;
using FluentValidation;

namespace BackendApi.Application.Validators;

public class CreateRecipeDtoValidator : AbstractValidator<CreateRecipeDto>
{
    public CreateRecipeDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tarif adı zorunludur")
            .MinimumLength(3).WithMessage("Tarif adı en az 3 karakter olmalıdır")
            .MaximumLength(200).WithMessage("Tarif adı en fazla 200 karakter olabilir");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Açıklama en fazla 1000 karakter olabilir");

        RuleFor(x => x.Ingredients)
            .NotEmpty().WithMessage("Malzemeler zorunludur")
            .MinimumLength(10).WithMessage("Malzemeler en az 10 karakter olmalıdır");

        RuleFor(x => x.Steps)
            .NotEmpty().WithMessage("Yapılış adımları zorunludur")
            .MinimumLength(20).WithMessage("Yapılış adımları en az 20 karakter olmalıdır");

        RuleFor(x => x.PrepTimeMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Hazırlık süresi 0 veya daha büyük olmalıdır")
            .LessThanOrEqualTo(1440).WithMessage("Hazırlık süresi 1440 dakikadan fazla olamaz");

        RuleFor(x => x.CookingTimeMinutes)
            .GreaterThan(0).WithMessage("Pişirme süresi 1 dakikadan büyük olmalıdır")
            .LessThanOrEqualTo(1440).WithMessage("Pişirme süresi 1440 dakikadan fazla olamaz");

        RuleFor(x => x.Servings)
            .GreaterThan(0).WithMessage("Kişi sayısı 1'den büyük olmalıdır")
            .LessThanOrEqualTo(100).WithMessage("Kişi sayısı 100'den fazla olamaz");

        RuleFor(x => x.Difficulty)
            .NotEmpty().WithMessage("Zorluk seviyesi zorunludur")
            .Must(BeValidDifficulty).WithMessage("Zorluk seviyesi 'Kolay', 'Orta' veya 'Zor' olmalıdır");

        RuleFor(x => x.ImageUrl)
            .Must(BeValidUrl).WithMessage("Geçerli bir URL girin")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl))
            .MaximumLength(500).WithMessage("URL en fazla 500 karakter olabilir");

        RuleFor(x => x.Tips)
            .MaximumLength(1000).WithMessage("Püf noktaları en fazla 1000 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.Tips));

        RuleFor(x => x.AlternativeIngredients)
            .MaximumLength(1000).WithMessage("Alternatif malzemeler en fazla 1000 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.AlternativeIngredients));

        RuleFor(x => x.NutritionInfo)
            .MaximumLength(500).WithMessage("Beslenme bilgileri en fazla 500 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.NutritionInfo));
    }

    private bool BeValidDifficulty(string difficulty)
    {
        if (string.IsNullOrEmpty(difficulty))
            return false;

        var validDifficulties = new[] { "Kolay", "Orta", "Zor", "kolay", "orta", "zor" };
        return validDifficulties.Contains(difficulty);
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        // Absolute URI (http/https) kontrolü
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps;
        }

        // Relative path kontrolü (/ ile başlayan path'ler geçerlidir)
        if (url.StartsWith("/", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
}

