using FluentValidation;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.SendOTP;
using UNI_EDU_Backend.Application.Interfaces.Repositories;

namespace UNI_EDU_Backend.Application.Services.Users;

public class UserService(IUserRepository userRepo, IValidator<CheckPhoneUserRequest> checkPhoneValidator) : IUserService
{
    private readonly IUserRepository _userRepo = userRepo;
    private readonly IValidator<CheckPhoneUserRequest> _checkPhoneValidator = checkPhoneValidator;

    public async Task<CheckPhoneUserResponse> CheckPhoneNumberAsync(CheckPhoneUserRequest request)
    {
        await _checkPhoneValidator.EnsureValidAsync(request, CancellationToken.None);

        bool isExisted = await _userRepo.CheckPhoneNumber(request.PhoneNumber);

        CheckPhoneUserResponse response = new()
        {
            PhoneNumber = request.PhoneNumber,
            IsExist = isExisted,
        };

        return response;
    }
}
