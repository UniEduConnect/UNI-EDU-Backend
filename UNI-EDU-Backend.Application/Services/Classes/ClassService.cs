using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Classes;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Classes;

public class ClassService(IClassRepository classRepo, IValidator<CreateClassRequest> createValidator) : IClassService
{
    private readonly IClassRepository _classRepo = classRepo;
    private readonly IValidator<CreateClassRequest> _createValidator = createValidator;

    public async Task<ClassResponse> CreateClassAsync(CreateClassRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        if (!await _classRepo.TutorExistsAsync(request.TutorId, cancellationToken))
            throw new NotFoundException($"Tutor with id '{request.TutorId}' not found.");

        if (!await _classRepo.StudentExistsAsync(request.StudentId, cancellationToken))
            throw new NotFoundException($"Student with id '{request.StudentId}' not found.");

        return await _classRepo.CreateClassWithEscrowAsync(request, cancellationToken);
    }
}
