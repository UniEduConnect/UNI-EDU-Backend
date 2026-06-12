namespace UNI_EDU_Backend.Application.DTOs.Profile;

// Common User fields any role can edit. Null fields are left unchanged.
public class UpdateMeRequest
{
    public string? Fullname { get; set; }
    public string? PhoneNumber { get; set; }
}
