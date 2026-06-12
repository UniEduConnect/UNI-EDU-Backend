using UNI_EDU_Backend.Application.DTOs.Reports;

namespace UNI_EDU_Backend.Application.Interfaces.Repositories;

public interface IReportRepository
{
    Task<AdminReportResponse> GetAdminReportAsync(CancellationToken cancellationToken);
    Task<FinanceReportResponse> GetFinanceReportAsync(CancellationToken cancellationToken);
    Task<OfficeReportResponse> GetOfficeReportAsync(CancellationToken cancellationToken);
    Task<StudentReportResponse> GetStudentReportAsync(Guid studentId, CancellationToken cancellationToken);
    Task<ParentReportResponse> GetParentReportAsync(Guid parentId, CancellationToken cancellationToken);
}
