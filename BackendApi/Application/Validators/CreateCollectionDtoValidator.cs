using BackendApi.Application.DTOs;
using FluentValidation;

namespace BackendApi.Application.Validators;

public class CreateCollectionDtoValidator : AbstractValidator<CreateCollectionDto>
{
    public CreateCollectionDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Koleksiyon adı gereklidir")
            .MaximumLength(100).WithMessage("Koleksiyon adı en fazla 100 karakter olabilir");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

