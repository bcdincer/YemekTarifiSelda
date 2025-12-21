using BackendApi.Application.DTOs;
using FluentValidation;

namespace BackendApi.Application.Validators;

public class CategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kategori adı zorunludur")
            .MinimumLength(2).WithMessage("Kategori adı en az 2 karakter olmalıdır")
            .MaximumLength(100).WithMessage("Kategori adı en fazla 100 karakter olabilir");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Icon)
            .MaximumLength(100).WithMessage("Icon sınıfı en fazla 100 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.Icon));

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Görüntüleme sırası 0 veya daha büyük olmalıdır");
    }
}

