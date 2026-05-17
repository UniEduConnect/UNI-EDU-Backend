using UNI_EDU_Backend.Application.DTOs.Request.Authentication;
using UNI_EDU_Backend.Application.DTOs.Response;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Application.Interfaces
{
    public interface IAuthService
    {
        Task<User> RegisterTutorAsync(TutorRegister registerDto);
        Task<User> RegisterStudentAsync(StudentRegister registerDto);
        Task<TokenResponse> LoginAsync(LoginRequest loginRequest);
    }
}