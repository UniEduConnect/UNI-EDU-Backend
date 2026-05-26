using Mapster;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.DTOs.Parents;

public class StudentSummaryResponse
{
    [AdaptMember(nameof(Student.StudentID))]
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
    public int Grade { get; set; }
}
