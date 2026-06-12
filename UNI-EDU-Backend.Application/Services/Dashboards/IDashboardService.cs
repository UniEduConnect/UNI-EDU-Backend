using UNI_EDU_Backend.Application.DTOs.Dashboards;

namespace UNI_EDU_Backend.Application.Services.Dashboards;

public interface IDashboardService
{
    Task<TutorDashboardResponse> GetTutorDashboardAsync(Guid tutorId, CancellationToken cancellationToken);
    Task<StudentDashboardResponse> GetStudentDashboardAsync(Guid studentId, CancellationToken cancellationToken);
    Task<ParentDashboardResponse> GetParentDashboardAsync(Guid parentId, CancellationToken cancellationToken);
    Task<FinanceDashboardResponse> GetFinanceDashboardAsync(CancellationToken cancellationToken);
    Task<OfficeDashboardResponse> GetOfficeDashboardAsync(CancellationToken cancellationToken);
    Task<ExamsDashboardResponse> GetExamsDashboardAsync(CancellationToken cancellationToken);
}
