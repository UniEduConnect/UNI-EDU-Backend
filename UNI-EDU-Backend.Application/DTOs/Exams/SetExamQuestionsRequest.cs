namespace UNI_EDU_Backend.Application.DTOs.Exams;

// Replaces the exam's full question set. Order in the list becomes QuestionOrder.
public class SetExamQuestionsRequest
{
    public List<int> QuestionIds { get; set; } = [];
}
