using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.Commons;

// Central place for the string <-> enum conventions the exam/question API speaks on the wire,
// plus the option-letter <-> zero-based-index bridge (Question stores OptionA..D + a letter answer,
// while the frontend works with an options array + numeric index).
public static class ExamMappings
{
    public static string ToWire(QuestionType type) => type switch
    {
        QuestionType.Essay => "essay",
        _ => "multiple-choice"
    };

    public static QuestionType ParseQuestionType(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() == "essay"
            ? QuestionType.Essay
            : QuestionType.MultipleChoice;

    public static string ToWire(DifficultyLevel level) => level switch
    {
        DifficultyLevel.Easy => "easy",
        DifficultyLevel.Hard => "hard",
        _ => "medium"
    };

    public static DifficultyLevel ParseDifficulty(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "easy" => DifficultyLevel.Easy,
            "hard" => DifficultyLevel.Hard,
            _ => DifficultyLevel.Medium
        };

    public static string ToWire(ExamStatus status) => status switch
    {
        ExamStatus.Open => "open",
        ExamStatus.Closed => "closed",
        ExamStatus.Hidden => "hidden",
        _ => "draft"
    };

    public static ExamStatus ParseExamStatus(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "open" => ExamStatus.Open,
            "closed" => ExamStatus.Closed,
            "hidden" => ExamStatus.Hidden,
            _ => ExamStatus.Draft
        };

    public static string ToWire(ExamType type) => type switch
    {
        ExamType.TutorTest => "tutor-test",
        _ => "student-test"
    };

    public static ExamType ParseExamType(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() == "tutor-test"
            ? ExamType.TutorTest
            : ExamType.StudentTest;

    public static string ToWire(CreatorType creator) => creator switch
    {
        CreatorType.AI => "ai",
        CreatorType.Tutor => "tutor",
        _ => "admin"
    };

    public static CreatorType ParseCreatorType(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "ai" => CreatorType.AI,
            "tutor" => CreatorType.Tutor,
            _ => CreatorType.Admin
        };

    // Stored answer key is a letter ("A".."D"); the wire uses a 0-based index.
    private static readonly string[] Letters = ["A", "B", "C", "D"];

    public static string LetterFromIndex(int index) =>
        index >= 0 && index < Letters.Length ? Letters[index] : "A";

    public static int? IndexFromLetter(string? letter)
    {
        if (string.IsNullOrWhiteSpace(letter)) return null;
        var i = Array.IndexOf(Letters, letter.Trim().ToUpperInvariant());
        return i >= 0 ? i : null;
    }
}
