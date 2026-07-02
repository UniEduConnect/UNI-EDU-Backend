using UNI_EDU_Backend.Application.DTOs.Request.Authentication;
using UNI_EDU_Backend.Application.DTOs.Response;

namespace UNI_EDU_Backend.Application.Services.Auths
{
    public interface IAuthService
    {
        Task<bool> RegisterTutorAsync(TutorRegister registerDto);

        Task<bool> RegisterStudentAsync(StudentRegister registerDto);

        Task<bool> RegisterParentAsync(ParentRegister registerDto);

        Task<TokenResponse> LoginAsync(LoginRequest loginRequest);

        Task<TokenResponse> RefreshTokenAsync(string request);

        // Clears the refresh-token cookie on logout — must use the SAME CookieOptions used
        // to set it, otherwise the browser treats it as a different cookie and never deletes
        // the original (see AuthService.BuildRefreshTokenCookieOptions).
        void ClearRefreshTokenCookie();
    }
}