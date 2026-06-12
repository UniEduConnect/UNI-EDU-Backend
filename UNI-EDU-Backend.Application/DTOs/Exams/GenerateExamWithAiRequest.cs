using FluentValidation;

namespace UNI_EDU_Backend.Application.DTOs.Exams;

// Spec for AI-generating an exam/exercise (tutor or exam-manager).
public class GenerateExamWithAiRequest
{
    public Guid SubjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Topic { get; set; }
    public int? Grade { get; set; }
    public string Difficulty { get; set; } = "medium"; // easy | medium | hard
    public int QuestionCount { get; set; } = 10;
    public int Duration { get; set; } = 30; // minutes
}

public class GenerateExamWithAiRequestValidator : AbstractValidator<GenerateExamWithAiRequest>
{
    public GenerateExamWithAiRequestValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty().WithMessage("Vui lòng chọn môn học.");
        RuleFor(x => x.Title).NotEmpty().WithMessage("Vui lòng nhập tiêu đề.").MaximumLength(200);
        RuleFor(x => x.QuestionCount).InclusiveBetween(1, 50).WithMessage("Số câu hỏi từ 1 đến 50.");
        RuleFor(x => x.Duration).InclusiveBetween(5, 240).WithMessage("Thời lượng từ 5 đến 240 phút.");
    }
}
