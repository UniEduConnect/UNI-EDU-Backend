using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Tutors;

public class TutorReviewsQueryValidator : AbstractValidator<TutorReviewsQuery>
{
    public TutorReviewsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page must be >= 1.");
    }
}
