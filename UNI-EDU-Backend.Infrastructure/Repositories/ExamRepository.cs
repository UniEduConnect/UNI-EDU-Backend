using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Exams;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Application.Interfaces.Repositories;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Repositories;

public class ExamRepository(ApplicationDbContext dbContext) : IExamRepository
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public Task<bool> SubjectExistsAsync(Guid subjectId, CancellationToken cancellationToken) =>
        _dbContext.Subjects.AnyAsync(s => s.SubjectID == subjectId, cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        _dbContext.Exams.AnyAsync(e => e.ExamID == id, cancellationToken);

    public async Task<(List<ExamListItemResponse> Items, int Total)> SearchAsync(ExamListQuery query, bool includeHidden, int pageSize, CancellationToken cancellationToken)
    {
        var page = query.Page < 1 ? 1 : query.Page;

        var q = _dbContext.Exams.AsNoTracking().AsQueryable();

        // Non-admin listings only surface open/closed exams (no drafts/hidden).
        if (!includeHidden)
            q = q.Where(e => e.Status == ExamStatus.Open || e.Status == ExamStatus.Closed);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(e => EF.Functions.ILike(e.Title, $"%{term}%"));
        }

        if (query.SubjectId is Guid subjectId)
            q = q.Where(e => e.SubjectID == subjectId);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = ExamMappings.ParseExamStatus(query.Status);
            q = q.Where(e => e.Status == status);
        }

        var total = await q.CountAsync(cancellationToken);

        var rows = await q
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new ExamRow(e, e.Subject.SubjectName, e.ExamQuestions.Count, e.Submissions.Count))
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => MapList(r)).ToList();
        return (items, total);
    }

    public async Task<ExamDetailResponse?> GetByIdAsync(int id, bool includeAnswerKey, CancellationToken cancellationToken)
    {
        var row = await _dbContext.Exams
            .AsNoTracking()
            .Where(e => e.ExamID == id)
            .Select(e => new ExamRow(e, e.Subject.SubjectName, e.ExamQuestions.Count, e.Submissions.Count))
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null) return null;

        var questionRows = await _dbContext.ExamQuestions
            .AsNoTracking()
            .Where(eq => eq.ExamID == id)
            .OrderBy(eq => eq.QuestionOrder)
            .Select(eq => new { eq.Question, SubjectName = eq.Question.Subject.SubjectName })
            .ToListAsync(cancellationToken);

        var detail = MapDetail(row);
        detail.Questions = questionRows
            .Select(r => QuestionRepository.Map(r.Question, r.SubjectName, includeAnswerKey))
            .ToList();

        return detail;
    }

    public async Task<ExamDetailResponse> CreateAsync(CreateExamRequest request, CancellationToken cancellationToken)
    {
        var entity = new Exam
        {
            SubjectID = request.SubjectId,
            Title = request.Title,
            Description = request.Description,
            Duration = request.Duration,
            Type = ExamMappings.ParseExamType(request.Type),
            CreatedBy = ExamMappings.ParseCreatorType(request.CreatedBy),
            Status = ExamMappings.ParseExamStatus(request.Status),
            Difficulty = ExamMappings.ParseDifficulty(request.Difficulty),
            Fee = request.Fee,
            Year = request.Year,
            StartDate = ToUtc(request.StartDate),
            EndDate = ToUtc(request.EndDate),
            MaxAttemptsPerUser = request.MaxAttemptsPerUser,
            ScoreScale = request.ScoreScale,
            AiProctoring = request.AiProctoring,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Exams.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.QuestionIds is { Count: > 0 })
        {
            AddExamQuestions(entity.ExamID, request.QuestionIds);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return (await GetByIdAsync(entity.ExamID, includeAnswerKey: true, cancellationToken))!;
    }

    public async Task<ExamDetailResponse> CreateAiExamAsync(
        Guid subjectId, string title, int duration, DifficultyLevel difficulty,
        CreatorType createdBy, Guid? creatorTutorId, string? topic,
        IReadOnlyList<GeneratedQuestion> questions, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Atomic: questions + exam + links commit together (or roll back) — no orphaned questions.
        await using var dbTx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var questionEntities = questions.Select(q => new Question
        {
            SubjectID = subjectId,
            Content = q.Content,
            Type = QuestionType.MultipleChoice,
            Difficulty = difficulty,
            OptionA = q.Options.ElementAtOrDefault(0) ?? string.Empty,
            OptionB = q.Options.ElementAtOrDefault(1) ?? string.Empty,
            OptionC = q.Options.ElementAtOrDefault(2) ?? string.Empty,
            OptionD = q.Options.ElementAtOrDefault(3) ?? string.Empty,
            CorrectAnswer = q.CorrectIndex switch { 1 => "B", 2 => "C", 3 => "D", _ => "A" },
            Topic = topic,
            CreatedAt = now,
        }).ToList();

        _dbContext.Questions.AddRange(questionEntities);
        await _dbContext.SaveChangesAsync(cancellationToken); // assigns QuestionIDs

        var exam = new Exam
        {
            SubjectID = subjectId,
            Title = title,
            Description = string.Empty,
            Duration = duration,
            Type = ExamType.StudentTest,
            CreatedBy = createdBy,
            Status = ExamStatus.Draft,
            Difficulty = difficulty,
            Fee = 0,
            Year = now.Year,
            MaxAttemptsPerUser = 1,
            ScoreScale = 10,
            AiProctoring = false,
            CreatorTutorID = creatorTutorId,
            CreatedAt = now,
        };
        _dbContext.Exams.Add(exam);
        await _dbContext.SaveChangesAsync(cancellationToken); // assigns ExamID

        AddExamQuestions(exam.ExamID, questionEntities.Select(q => q.QuestionID).ToList());
        await _dbContext.SaveChangesAsync(cancellationToken);

        var result = (await GetByIdAsync(exam.ExamID, includeAnswerKey: true, cancellationToken))!;
        await dbTx.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<ExamDetailResponse?> UpdateAsync(int id, UpdateExamRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Exams.FirstOrDefaultAsync(e => e.ExamID == id, cancellationToken);
        if (entity is null) return null;

        entity.SubjectID = request.SubjectId;
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.Duration = request.Duration;
        entity.Type = ExamMappings.ParseExamType(request.Type);
        entity.Status = ExamMappings.ParseExamStatus(request.Status);
        entity.Difficulty = ExamMappings.ParseDifficulty(request.Difficulty);
        entity.Fee = request.Fee;
        entity.Year = request.Year;
        entity.StartDate = ToUtc(request.StartDate);
        entity.EndDate = ToUtc(request.EndDate);
        entity.MaxAttemptsPerUser = request.MaxAttemptsPerUser;
        entity.ScoreScale = request.ScoreScale;
        entity.AiProctoring = request.AiProctoring;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, includeAnswerKey: true, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Exams.FirstOrDefaultAsync(e => e.ExamID == id, cancellationToken);
        if (entity is null) return false;

        // ExamQuestions + Submissions are removed by the DB cascade on ExamID.
        _dbContext.Exams.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<int>> GetMissingQuestionIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken)
    {
        var distinct = ids.Distinct().ToList();
        var existing = await _dbContext.Questions
            .Where(q => distinct.Contains(q.QuestionID))
            .Select(q => q.QuestionID)
            .ToListAsync(cancellationToken);

        return distinct.Except(existing).ToList();
    }

    public async Task<ExamDetailResponse?> SetQuestionsAsync(int examId, List<int> questionIds, CancellationToken cancellationToken)
    {
        if (!await ExistsAsync(examId, cancellationToken)) return null;

        var existing = await _dbContext.ExamQuestions
            .Where(eq => eq.ExamID == examId)
            .ToListAsync(cancellationToken);
        _dbContext.ExamQuestions.RemoveRange(existing);

        AddExamQuestions(examId, questionIds);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(examId, includeAnswerKey: true, cancellationToken);
    }

    public async Task<ExamGradeContext?> GetGradeContextAsync(int examId, CancellationToken cancellationToken)
    {
        var exam = await _dbContext.Exams
            .AsNoTracking()
            .Where(e => e.ExamID == examId)
            .Select(e => new { e.ExamID, e.Title, e.Status, e.ScoreScale, e.MaxAttemptsPerUser })
            .FirstOrDefaultAsync(cancellationToken);

        if (exam is null) return null;

        var questionRows = await _dbContext.ExamQuestions
            .AsNoTracking()
            .Where(eq => eq.ExamID == examId)
            .OrderBy(eq => eq.QuestionOrder)
            .Select(eq => new { eq.QuestionID, eq.Question.CorrectAnswer, eq.Question.Type })
            .ToListAsync(cancellationToken);

        var graded = questionRows
            .Select(q => new GradedQuestion(
                q.QuestionID,
                q.Type == QuestionType.Essay ? null : ExamMappings.IndexFromLetter(q.CorrectAnswer)))
            .ToList();

        return new ExamGradeContext(exam.ExamID, exam.Title, exam.Status, exam.ScoreScale, exam.MaxAttemptsPerUser, graded);
    }

    public Task<int> CountUserAttemptsAsync(int examId, Guid userId, CancellationToken cancellationToken) =>
        _dbContext.Submissions.CountAsync(s => s.ExamID == examId && s.UserID == userId, cancellationToken);

    public async Task<SubmissionResponse> CreateSubmissionAsync(int examId, Guid userId, string answersJson, float score, int correctCount, int totalQuestions, CancellationToken cancellationToken)
    {
        var entity = new Submission
        {
            ExamID = examId,
            UserID = userId,
            SubmissionDate = DateTime.UtcNow,
            Answers = answersJson,
            Score = score,
            CorrectCount = correctCount,
            TotalQuestions = totalQuestions,
            AIFeedback = null
        };

        _dbContext.Submissions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _dbContext.Submissions
            .AsNoTracking()
            .Where(s => s.SubmissionID == entity.SubmissionID)
            .Select(SubmissionProjection)
            .FirstAsync(cancellationToken);
    }

    public async Task<(List<SubmissionResponse> Items, int Total)> GetSubmissionsByExamAsync(int examId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.Submissions.AsNoTracking().Where(s => s.ExamID == examId);
        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(s => s.SubmissionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(SubmissionProjection)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(List<SubmissionResponse> Items, int Total)> GetSubmissionsByUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var q = _dbContext.Submissions.AsNoTracking().Where(s => s.UserID == userId);
        var total = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(s => s.SubmissionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(SubmissionProjection)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<List<ExamStatItem>> GetStatsAsync(CancellationToken cancellationToken)
    {
        var rows = await _dbContext.Exams
            .AsNoTracking()
            .Select(e => new
            {
                e.ExamID,
                e.Title,
                Subject = e.Subject.SubjectName,
                e.ScoreScale,
                Attempts = e.Submissions.Count(),
                SumScore = e.Submissions.Sum(s => (double?)s.Score) ?? 0d,
                PassCount = e.Submissions.Count(s => s.Score >= e.ScoreScale / 2.0)
            })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new ExamStatItem
        {
            ExamId = r.ExamID,
            Title = r.Title,
            Subject = r.Subject,
            ScoreScale = r.ScoreScale,
            Attempts = r.Attempts,
            AvgScore = r.Attempts > 0 ? Math.Round(r.SumScore / r.Attempts, 2) : 0d,
            PassRate = r.Attempts > 0 ? Math.Round(r.PassCount * 100.0 / r.Attempts, 1) : 0d
        }).ToList();
    }

    public async Task<ExamAiConfigResponse> GetAiConfigAsync(CancellationToken cancellationToken)
    {
        var config = await GetOrCreateAiConfigAsync(cancellationToken);
        return MapAiConfig(config);
    }

    public async Task<ExamAiConfigResponse> UpdateAiConfigAsync(UpdateExamAiConfigRequest request, CancellationToken cancellationToken)
    {
        var c = await GetOrCreateAiConfigAsync(cancellationToken);
        c.ProctoringEnabled = request.ProctoringEnabled;
        c.FaceDetection = request.FaceDetection;
        c.FullscreenRequired = request.FullscreenRequired;
        c.CopyPasteBlocked = request.CopyPasteBlocked;
        c.TabSwitchLimit = request.TabSwitchLimit;
        c.AutoGenerateEnabled = request.AutoGenerateEnabled;
        c.DefaultDifficulty = (request.DefaultDifficulty ?? "medium").Trim().ToLowerInvariant();
        c.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapAiConfig(c);
    }

    private async Task<ExamAiConfig> GetOrCreateAiConfigAsync(CancellationToken cancellationToken)
    {
        var config = await _dbContext.ExamAiConfigs.FirstOrDefaultAsync(cancellationToken);
        if (config is null)
        {
            config = new ExamAiConfig { ConfigID = Guid.NewGuid(), UpdatedAt = DateTime.UtcNow };
            _dbContext.ExamAiConfigs.Add(config);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        return config;
    }

    private static ExamAiConfigResponse MapAiConfig(ExamAiConfig c) => new()
    {
        ProctoringEnabled = c.ProctoringEnabled,
        FaceDetection = c.FaceDetection,
        FullscreenRequired = c.FullscreenRequired,
        CopyPasteBlocked = c.CopyPasteBlocked,
        TabSwitchLimit = c.TabSwitchLimit,
        AutoGenerateEnabled = c.AutoGenerateEnabled,
        DefaultDifficulty = c.DefaultDifficulty,
        UpdatedAt = c.UpdatedAt
    };

    private void AddExamQuestions(int examId, List<int> questionIds)
    {
        var order = 1;
        foreach (var qid in questionIds)
        {
            _dbContext.ExamQuestions.Add(new ExamQuestion
            {
                ExamID = examId,
                QuestionID = qid,
                QuestionOrder = order++
            });
        }
    }

    private static DateTime? ToUtc(DateTime? value) =>
        value is null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);

    private static readonly System.Linq.Expressions.Expression<Func<Submission, SubmissionResponse>> SubmissionProjection =
        s => new SubmissionResponse
        {
            Id = s.SubmissionID,
            ExamId = s.ExamID,
            ExamTitle = s.Exam.Title,
            UserId = s.UserID,
            StudentName = s.User.Fullname,
            Score = s.Score,
            ScoreScale = s.Exam.ScoreScale,
            TotalQuestions = s.TotalQuestions,
            CorrectCount = s.CorrectCount,
            SubmissionDate = s.SubmissionDate,
            AIFeedback = s.AIFeedback
        };

    private record ExamRow(Exam Exam, string SubjectName, int QuestionCount, int Attempts);

    private static ExamDetailResponse MapDetail(ExamRow r)
    {
        var d = new ExamDetailResponse { Description = r.Exam.Description };
        Fill(d, r);
        return d;
    }

    private static ExamListItemResponse MapList(ExamRow r)
    {
        var item = new ExamListItemResponse();
        Fill(item, r);
        return item;
    }

    private static void Fill(ExamListItemResponse item, ExamRow r)
    {
        var e = r.Exam;
        item.Id = e.ExamID;
        item.Title = e.Title;
        item.SubjectId = e.SubjectID;
        item.Subject = r.SubjectName;
        item.Duration = e.Duration;
        item.QuestionCount = r.QuestionCount;
        item.Fee = e.Fee;
        item.Year = e.Year;
        item.Status = ExamMappings.ToWire(e.Status);
        item.Difficulty = ExamMappings.ToWire(e.Difficulty);
        item.Type = ExamMappings.ToWire(e.Type);
        item.CreatedBy = ExamMappings.ToWire(e.CreatedBy);
        item.AiProctoring = e.AiProctoring;
        item.StartDate = e.StartDate;
        item.EndDate = e.EndDate;
        item.MaxAttemptsPerUser = e.MaxAttemptsPerUser;
        item.ScoreScale = e.ScoreScale;
        item.Attempts = r.Attempts;
        item.CreatedAt = e.CreatedAt;
    }
}
