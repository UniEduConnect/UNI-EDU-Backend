using UNI_EDU_Backend.Application.DTOs.Reports;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Reports;

public class ReportService(IReportRepository reportRepo) : IReportService
{
    private readonly IReportRepository _reportRepo = reportRepo;

    public Task<AdminReportResponse> GetAdminReportAsync(CancellationToken cancellationToken) => _reportRepo.GetAdminReportAsync(cancellationToken);
    public Task<FinanceReportResponse> GetFinanceReportAsync(CancellationToken cancellationToken) => _reportRepo.GetFinanceReportAsync(cancellationToken);
    public Task<OfficeReportResponse> GetOfficeReportAsync(CancellationToken cancellationToken) => _reportRepo.GetOfficeReportAsync(cancellationToken);
    public Task<StudentReportResponse> GetStudentReportAsync(Guid studentId, CancellationToken cancellationToken) => _reportRepo.GetStudentReportAsync(studentId, cancellationToken);
    public Task<ParentReportResponse> GetParentReportAsync(Guid parentId, CancellationToken cancellationToken) => _reportRepo.GetParentReportAsync(parentId, cancellationToken);
}
