using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Questions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class QuestionRepository(ApplicationDbContext dbContext) : IQuestionRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public Task<bool> SubjectExistsAsync(Guid subjectId, CancellationToken cancellationToken) =>
        _dbContext.Subjects.AnyAsync(s => s.SubjectID == subjectId, cancellationToken);

    public async Task<(List<QuestionResponse> Items, int Total)> SearchAsync(QuestionListQuery query, int pageSize, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;

        var q = _dbContext.Questions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(x => EF.Functions.ILike(x.Content, $"%{term}%"));
        }

        if (query.SubjectId is Guid subjectId)
            q = q.Where(x => x.SubjectID == subjectId);

        if (!string.IsNullOrWhiteSpace(query.Difficulty))
        {
            var diff = ExamMappings.ParseDifficulty(query.Difficulty);
            q = q.Where(x => x.Difficulty == diff);
        }

        var total = await q.CountAsync(cancellationToken);

        var rows = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new { Question = x, SubjectName = x.Subject.SubjectName })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => Map(r.Question, r.SubjectName, includeAnswer: true)).ToList();
        return (items, total);
    }

    public async Task<QuestionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var row = await _dbContext.Questions
            .AsNoTracking()
            .Where(x => x.QuestionID == id)
            .Select(x => new { Question = x, SubjectName = x.Subject.SubjectName })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : Map(row.Question, row.SubjectName, includeAnswer: true);
    }

    public async Task<QuestionResponse> CreateAsync(CreateQuestionRequest request, CancellationToken cancellationToken)
    {
        var (a, b, c, d) = SplitOptions(request.Options);

        var entity = new Question
        {
            SubjectID = request.SubjectId,
            Content = request.Content,
            Type = ExamMappings.ParseQuestionType(request.Type),
            Difficulty = ExamMappings.ParseDifficulty(request.Difficulty),
            OptionA = a,
            OptionB = b,
            OptionC = c,
            OptionD = d,
            CorrectAnswer = ExamMappings.LetterFromIndex(request.CorrectAnswer),
            Topic = request.Topic,
            Standard = request.Standard,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Questions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var subjectName = await SubjectNameAsync(entity.SubjectID, cancellationToken);
        return Map(entity, subjectName, includeAnswer: true);
    }

    public async Task<QuestionResponse?> UpdateAsync(int id, UpdateQuestionRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Questions.FirstOrDefaultAsync(x => x.QuestionID == id, cancellationToken);
        if (entity is null) return null;

        var (a, b, c, d) = SplitOptions(request.Options);

        entity.SubjectID = request.SubjectId;
        entity.Content = request.Content;
        entity.Type = ExamMappings.ParseQuestionType(request.Type);
        entity.Difficulty = ExamMappings.ParseDifficulty(request.Difficulty);
        entity.OptionA = a;
        entity.OptionB = b;
        entity.OptionC = c;
        entity.OptionD = d;
        entity.CorrectAnswer = ExamMappings.LetterFromIndex(request.CorrectAnswer);
        entity.Topic = request.Topic;
        entity.Standard = request.Standard;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var subjectName = await SubjectNameAsync(entity.SubjectID, cancellationToken);
        return Map(entity, subjectName, includeAnswer: true);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Questions.FirstOrDefaultAsync(x => x.QuestionID == id, cancellationToken);
        if (entity is null) return false;

        _dbContext.Questions.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Task<string> SubjectNameAsync(Guid subjectId, CancellationToken cancellationToken) =>
        _dbContext.Subjects
            .Where(s => s.SubjectID == subjectId)
            .Select(s => s.SubjectName)
            .FirstOrDefaultAsync(cancellationToken)!;

    private static (string A, string B, string C, string D) SplitOptions(List<string>? options)
    {
        options ??= [];
        string At(int i) => i < options.Count ? options[i] ?? string.Empty : string.Empty;
        return (At(0), At(1), At(2), At(3));
    }

    // Shared mapping used here and by ExamRepository.
    internal static QuestionResponse Map(Question x, string? subjectName, bool includeAnswer) => new()
    {
        Id = x.QuestionID,
        SubjectId = x.SubjectID,
        Subject = subjectName ?? string.Empty,
        Content = x.Content,
        Type = ExamMappings.ToWire(x.Type),
        Difficulty = ExamMappings.ToWire(x.Difficulty),
        Options = [x.OptionA ?? string.Empty, x.OptionB ?? string.Empty, x.OptionC ?? string.Empty, x.OptionD ?? string.Empty],
        CorrectAnswer = includeAnswer ? ExamMappings.IndexFromLetter(x.CorrectAnswer) : null,
        Topic = x.Topic,
        Standard = x.Standard,
        CreatedAt = x.CreatedAt
    };
}
