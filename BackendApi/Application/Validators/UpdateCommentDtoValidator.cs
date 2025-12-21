using BackendApi.Application.DTOs;
using FluentValidation;

namespace BackendApi.Application.Validators;

public class UpdateCommentDtoValidator : AbstractValidator<UpdateCommentDto>
{
    public UpdateCommentDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Yorum içeriği zorunludur.")
            .MinimumLength(1).WithMessage("Yorum en az 1 karakter olmalıdır.")
            .MaximumLength(1000).WithMessage("Yorum en fazla 1000 karakter olabilir.");
    }
}

