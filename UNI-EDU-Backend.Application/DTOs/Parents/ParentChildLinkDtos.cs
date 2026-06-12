using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Parents;

// Body of POST /api/Parents/me/children/link — a parent links to a student by email.
public class LinkChildRequest
{
    public string ChildEmail { get; set; } = string.Empty;
}

public class LinkChildRequestValidator : AbstractValidator<LinkChildRequest>
{
    public LinkChildRequestValidator()
    {
        RuleFor(x => x.ChildEmail)
            .NotEmpty().WithMessage("Vui lòng nhập email của con.")
            .EmailAddress().WithMessage("Email không hợp lệ.");
    }
}

// Body of POST /api/Parents/me/children/{childId}/fund — parent tops up a child's wallet.
public class FundChildRequest
{
    public decimal Amount { get; set; }
}

// A pending parent-link request shown to the student for approval.
public class ParentLinkRequestResponse
{
    public Guid Id { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
}
