namespace UNI_EDU_Backend.Application.DTOs.Exams;

public class SubmitExamRequest
{
    public List<ExamAnswerDto> Answers { get; set; } = [];
}

public class ExamAnswerDto
{
    public int QuestionId { get; set; }

    // Zero-based index into the question's options.
    public int SelectedOption { get; set; }
}
