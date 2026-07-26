using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.TutorPosts;

namespace UNI_EDU_Backend.Application.Services.TutorPosts;

public interface ITutorPostService
{
    Task CreateAsync(Guid tutorId, CreateTutorPostRequest request, CancellationToken cancellationToken);
    Task<PagedResult<TutorPostResponse>> GetOpenAsync(TutorPostListQuery query, Guid callerId, CancellationToken cancellationToken);
    Task<List<TutorPostResponse>> GetMineAsync(Guid tutorId, CancellationToken cancellationToken);
    Task CloseAsync(Guid tutorId, Guid postId, CancellationToken cancellationToken);

    // A student registers on a tutor's post (notifies the tutor to take a test).
    Task ApplyAsync(Guid studentId, Guid postId, CancellationToken cancellationToken);
    // The tutor's pending applications.
    Task<List<TutorPostApplicationResponse>> GetApplicationsAsync(Guid tutorId, CancellationToken cancellationToken);
    // Tutor accepts an application after passing an AI test (>=80%) for the post subject.
    Task AcceptApplicationAsync(Guid tutorId, Guid appId, AcceptApplicationRequest request, CancellationToken cancellationToken);

    // Read-only pre-check: can this tutor accept this application (pending, owned, no clash, student
    // can pay)? Throws the same typed exceptions AcceptApplicationAsync would — runs BEFORE the AI test.
    Task EnsureApplicationAcceptableAsync(Guid tutorId, Guid appId, CancellationToken cancellationToken);
}
