namespace UNI_EDU_Backend.Application.Interfaces;

public record GeneratedQuestion(string Content, List<string> Options, int CorrectIndex);

public interface IAiQuestionGenerator
{
    // Generate `count` multiple-choice questions assessing competence in `subjectName`.
    Task<List<GeneratedQuestion>> GenerateAsync(string subjectName, int count, CancellationToken cancellationToken);

    // Richer generation for the "create exercise/exam with AI" flow (topic/grade/difficulty woven into the prompt).
    Task<List<GeneratedQuestion>> GenerateAsync(string subjectName, int count, string? topic, int? grade, string? difficulty, CancellationToken cancellationToken);
}
