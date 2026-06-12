using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.DTOs.Exams;

// Minimal data the service needs to grade a submission, without leaking the answer key to the wire.
public record ExamGradeContext(
    int ExamId,
    string Title,
    ExamStatus Status,
    int ScoreScale,
    int MaxAttemptsPerUser,
    IReadOnlyList<GradedQuestion> Questions);

// CorrectIndex is the 0-based option index of the correct answer (null for essay questions).
public record GradedQuestion(int QuestionId, int? CorrectIndex);
