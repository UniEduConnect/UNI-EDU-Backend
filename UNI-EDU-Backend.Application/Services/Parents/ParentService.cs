using Mapster;
using UNI_EDU_Backend.Application.DTOs.Parents;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Parents;

public class ParentService(IStudentRepository studentRepository) : IParentService
{
    private readonly IStudentRepository _studentRepository = studentRepository;

    public async Task<List<StudentSummaryResponse>> GetMyChildrenAsync(Guid parentId, CancellationToken cancellationToken)
    {
        var children = await _studentRepository.GetByParentIdAsync(parentId, cancellationToken);
        return children.Adapt<List<StudentSummaryResponse>>();
    }
}
