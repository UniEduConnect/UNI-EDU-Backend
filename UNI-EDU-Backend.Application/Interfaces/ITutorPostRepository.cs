using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.TutorPosts;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface ITutorPostRepository
{
    Task CreateAsync(Guid tutorId, CreateTutorPostRequest request, CancellationToken cancellationToken);

    // Open posts students can browse. `callerId` marks each post with whether the caller has a
    // pending application on it (Parent callers never match a StudentId, so it's always false for them).
    Task<PagedResult<TutorPostResponse>> GetOpenAsync(TutorPostListQuery query, int pageSize, Guid callerId, CancellationToken cancellationToken);

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
    // PostStatus lets the service reject accepting into a post that's already closed
    // (e.g. a race between two pending applications on the same post).
    Task<(Guid SubjectId, Guid StudentId, string Status, string PostStatus)?> GetApplicationForAcceptAsync(Guid appId, Guid tutorId, CancellationToken cancellationToken);
    // Accepts the application, materializes the Class, and closes the post — a post is a
    // single "slot"; once it's matched into a class it drops off everyone's open-posts list.
    Task AcceptApplicationAsync(Guid appId, CancellationToken cancellationToken);
}
