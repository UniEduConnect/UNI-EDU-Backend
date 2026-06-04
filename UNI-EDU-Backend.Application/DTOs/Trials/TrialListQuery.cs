namespace UNI_EDU_Backend.Application.DTOs.Trials;

public class TrialListQuery
{
    // "pending" | "accepted" | "declined". Omit for all.
    public string? Status { get; set; }

    // 1-based. Default page size is 10 (see TrialService.PageSize).
    public int Page { get; set; } = 1;
}

public enum TrialReviewOutcome
{
    NotFound,
    Forbidden,
    AlreadyResponded,
    Done
}
