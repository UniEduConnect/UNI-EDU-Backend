using UNI_EDU_Backend.Application.DTOs.SendOTP;

namespace UNI_EDU_Backend.Application.Services.Users;

public interface IUserService
{
    public Task<CheckPhoneUserResponse> CheckPhoneNumberAsync(CheckPhoneUserRequest request);
}
