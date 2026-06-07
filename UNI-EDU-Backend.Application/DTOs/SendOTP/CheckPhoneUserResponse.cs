using UNI_EDU_Backend.Domain.Enums;

namespace UNI_EDU_Backend.Application.DTOs.SendOTP;

public class CheckPhoneUserResponse
{
    public string PhoneNumber { get; set; } = default!;

    public bool IsExist { get; set; }
}
