using UNI_EDU_Backend.Application.DTOs.Tutors;

namespace UNI_EDU_Backend.Application.Interfaces;

public interface ITutorRepository
{
    Task<(List<TutorListingResponse> Items, int Total)> SearchAsync(
        TutorSearchQuery query,
        int pageSize,
        CancellationToken cancellationToken);
}
