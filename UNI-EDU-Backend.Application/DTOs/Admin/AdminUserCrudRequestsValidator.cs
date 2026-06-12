using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Admin;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    private static readonly string[] AllowedRoles = ["student", "parent", "tutor", "teacher"];

    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

        RuleFor(x => x.Fullname)
            .NotEmpty().WithMessage("Fullname is required.")
            .MaximumLength(100).WithMessage("Fullname must not exceed 100 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^0\d{9}$").WithMessage("Phone number must be 10 digits starting with 0.");

        RuleFor(x => x.Role)
            .Must(r => !string.IsNullOrWhiteSpace(r) && AllowedRoles.Contains(r.Trim().ToLowerInvariant()))
            .WithMessage("Role must be one of: student, parent, tutor, teacher.");

        RuleFor(x => x.Grade)
            .InclusiveBetween(1, 12).WithMessage("Grade must be between 1 and 12.")
            .When(x => x.Grade.HasValue);
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email is not a valid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Fullname)
            .MaximumLength(100).WithMessage("Fullname must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Fullname));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^0\d{9}$").WithMessage("Phone number must be 10 digits starting with 0.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.Grade)
            .InclusiveBetween(1, 12).WithMessage("Grade must be between 1 and 12.")
            .When(x => x.Grade.HasValue);
    }
}
