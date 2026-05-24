using UNI_EDU_Backend.Application.DTOs.Classes;

namespace UNI_EDU_Backend.Application.Services.Classes;

public interface IClassService
{
    public Task<ClassResponse> CreateClassAsync(CreateClassRequest request, CancellationToken cancellationToken);
}
