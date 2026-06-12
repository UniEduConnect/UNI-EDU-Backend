namespace UNI_EDU_Backend.Application.DTOs.Withdrawals;

public enum WithdrawalReviewOutcome
{
    NotFound,
    AlreadyReviewed,
    Done
}

// Returned by Approve/Reject so the service can write a meaningful audit entry.
public record WithdrawalReviewResult(WithdrawalReviewOutcome Outcome, string TutorName, decimal Amount);
