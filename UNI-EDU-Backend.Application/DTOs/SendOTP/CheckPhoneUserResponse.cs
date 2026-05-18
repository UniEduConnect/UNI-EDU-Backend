using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.DTOs.SendOTP;

public class CheckPhoneUserResponse
{
    public Guid UserID { get; set; }

    public string Fullname { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string PhoneNumber { get; set; } = default!;

    public UserRole Role { get; set; }
}
