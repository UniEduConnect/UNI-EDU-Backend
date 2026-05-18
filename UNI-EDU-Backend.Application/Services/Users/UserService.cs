using Mapster;
using UNI_EDU_Backend.Application.DTOs.SendOTP;
using UNI_EDU_Backend.Application.Interfaces;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Services.Users;

public class UserService(IUserRepository userRepo) : IUserService
{
    private readonly IUserRepository _userRepo = userRepo;

    public async Task<CheckPhoneUserResponse> CheckPhoneNumberAsync(CheckPhoneUserRequest request)
    {
        User user = await _userRepo.CheckPhoneNumber(request.PhoneNumber);

        CheckPhoneUserResponse response = user.Adapt<CheckPhoneUserResponse>();

        return response;
    }
}
