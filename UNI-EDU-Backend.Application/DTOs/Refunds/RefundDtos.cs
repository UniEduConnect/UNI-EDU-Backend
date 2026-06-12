namespace UNI_EDU_Backend.Application.DTOs.Refunds;

public class CreateRefundRequest
{
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ReviewRefundRequest
{
    public string? Note { get; set; }
}

public class RefundListQuery
{
    // "pending" | "approved" | "rejected". Omit for all.
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
}

public class RefundResponse
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string TutorName { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal MaxAmount { get; set; }
    public string Reason { get; set; } = string.Empty;

    // "pending" | "approved" | "rejected"
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}

public enum RefundReviewOutcome { NotFound, AlreadyReviewed, Done }

public record RefundReviewResult(RefundReviewOutcome Outcome, string StudentName, decimal Amount);
