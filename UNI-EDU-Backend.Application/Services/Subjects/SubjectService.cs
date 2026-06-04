using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Subjects;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Subjects;

public class SubjectService(
    ISubjectRepository subjectRepo,
    IValidator<SaveSubjectRequest> saveValidator) : ISubjectService
{
    private readonly ISubjectRepository _subjectRepo = subjectRepo;
    private readonly IValidator<SaveSubjectRequest> _saveValidator = saveValidator;

    public Task<List<SubjectResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        _subjectRepo.GetAllAsync(cancellationToken);

    public async Task<SubjectResponse> CreateAsync(SaveSubjectRequest request, CancellationToken cancellationToken)
    {
        await _saveValidator.EnsureValidAsync(request, cancellationToken);

        if (await _subjectRepo.NameExistsAsync(request.Name, null, cancellationToken))
            throw new BadRequestException($"A subject named '{request.Name.Trim()}' already exists.");

        return await _subjectRepo.CreateAsync(request.Name, cancellationToken);
    }

    public async Task<SubjectResponse> UpdateAsync(Guid id, SaveSubjectRequest request, CancellationToken cancellationToken)
    {
        await _saveValidator.EnsureValidAsync(request, cancellationToken);

        if (await _subjectRepo.NameExistsAsync(request.Name, id, cancellationToken))
            throw new BadRequestException($"A subject named '{request.Name.Trim()}' already exists.");

        return await _subjectRepo.UpdateAsync(id, request.Name, cancellationToken)
            ?? throw new NotFoundException($"Subject with id '{id}' not found.");
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var outcome = await _subjectRepo.DeleteAsync(id, cancellationToken);
        if (outcome == SubjectDeleteOutcome.NotFound)
            throw new NotFoundException($"Subject with id '{id}' not found.");
        if (outcome == SubjectDeleteOutcome.InUse)
            throw new BadRequestException("This subject is still in use by classes, exams, questions, or tutors and cannot be deleted.");
    }
}
