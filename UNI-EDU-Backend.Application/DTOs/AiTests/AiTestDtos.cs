namespace UNI_EDU_Backend.Application.DTOs.AiTests;

public class GenerateAiTestRequest
{
    public Guid SubjectId { get; set; }
}

public class AiTestQuestionDto
{
    public int Index { get; set; }
    public string Content { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
}

public class AiTestResponse
{
    public Guid AttemptId { get; set; }
    public Guid SubjectId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public int PassThreshold { get; set; } = 80;
    public List<AiTestQuestionDto> Questions { get; set; } = new();
}

public class SubmitAiTestRequest
{
    // Selected option index per question, in order.
    public List<int> Answers { get; set; } = new();
}

public class AiTestResultResponse
{
    public Guid AttemptId { get; set; }
    public int Score { get; set; }          // percent 0-100
    public int CorrectCount { get; set; }
    public int Total { get; set; }
    public bool Passed { get; set; }
    public int PassThreshold { get; set; } = 80;
}
