using FluentValidation;
using PublicationQualitySystem.Application.DTOs.PaperReviewProfile;

namespace PublicationQualitySystem.Application.Validators.PaperReviewProfile;

public class CreatePaperReviewProfileRequestValidator : AbstractValidator<CreatePaperReviewProfileRequest>
{
    public CreatePaperReviewProfileRequestValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(1000);
    }
}
