using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.TutorPosts;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface ITutorPostRepository
{
    Task CreateAsync(Guid tutorId, CreateTutorPostRequest request, CancellationToken cancellationToken);

    // Open posts students can browse.
    Task<PagedResult<TutorPostResponse>> GetOpenAsync(TutorPostListQuery query, int pageSize, CancellationToken cancellationToken);

    // A tutor's own posts.
    Task<List<TutorPostResponse>> GetMineAsync(Guid tutorId, CancellationToken cancellationToken);

    // Closes a post the tutor owns. Returns false if not found / not owned.
    Task<bool> CloseAsync(Guid postId, Guid tutorId, CancellationToken cancellationToken);

    // --- Applications (a student registers on a tutor's post) ---
    Task<(Guid TutorId, Guid SubjectId)?> GetOpenPostForApplyAsync(Guid postId, CancellationToken cancellationToken);
    Task<bool> HasPendingApplicationAsync(Guid postId, Guid studentId, CancellationToken cancellationToken);
    Task CreateApplicationAsync(Guid postId, Guid studentId, Guid tutorId, Guid subjectId, CancellationToken cancellationToken);
    Task<string> GetStudentNameAsync(Guid studentId, CancellationToken cancellationToken);

    Task<List<TutorPostApplicationResponse>> GetApplicationsForTutorAsync(Guid tutorId, CancellationToken cancellationToken);
    Task<(Guid SubjectId, Guid StudentId, string Status)?> GetApplicationForAcceptAsync(Guid appId, Guid tutorId, CancellationToken cancellationToken);
    Task AcceptApplicationAsync(Guid appId, CancellationToken cancellationToken);
}
