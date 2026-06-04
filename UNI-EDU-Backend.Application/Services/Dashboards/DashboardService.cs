using UNI_EDU_Backend.Application.DTOs.Dashboards;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Dashboards;

public class DashboardService(IDashboardRepository dashboardRepo) : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepo = dashboardRepo;

    public Task<TutorDashboardResponse> GetTutorDashboardAsync(Guid tutorId, CancellationToken cancellationToken) =>
        _dashboardRepo.GetTutorDashboardAsync(tutorId, cancellationToken);

    public Task<StudentDashboardResponse> GetStudentDashboardAsync(Guid studentId, CancellationToken cancellationToken) =>
        _dashboardRepo.GetStudentDashboardAsync(studentId, cancellationToken);

    public Task<ParentDashboardResponse> GetParentDashboardAsync(Guid parentId, CancellationToken cancellationToken) =>
        _dashboardRepo.GetParentDashboardAsync(parentId, cancellationToken);

    public Task<FinanceDashboardResponse> GetFinanceDashboardAsync(CancellationToken cancellationToken) =>
        _dashboardRepo.GetFinanceDashboardAsync(cancellationToken);

    public Task<OfficeDashboardResponse> GetOfficeDashboardAsync(CancellationToken cancellationToken) =>
        _dashboardRepo.GetOfficeDashboardAsync(cancellationToken);

    public Task<ExamsDashboardResponse> GetExamsDashboardAsync(CancellationToken cancellationToken) =>
        _dashboardRepo.GetExamsDashboardAsync(cancellationToken);
}
