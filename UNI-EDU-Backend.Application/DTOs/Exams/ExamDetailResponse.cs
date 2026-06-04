using UNI_EDU_Backend.Application.DTOs.Questions;

namespace UNI_EDU_Backend.Application.DTOs.Exams;

public class ExamDetailResponse : ExamListItemResponse
{
    public string Description { get; set; } = string.Empty;

    // Ordered questions. CorrectAnswer is null on each item when the caller is taking the
    // exam (answer key hidden); populated for the creator/Admin.
    public List<QuestionResponse> Questions { get; set; } = [];
}
