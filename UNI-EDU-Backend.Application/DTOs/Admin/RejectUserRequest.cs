namespace UNI_EDU_Backend.Application.DTOs.Admin;

public class RejectUserRequest
{
    // Optional reason recorded in the audit log.
    public string? Reason { get; set; }
}
