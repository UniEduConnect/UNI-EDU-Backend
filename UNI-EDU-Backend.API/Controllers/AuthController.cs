using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.Request.Authentication;
using UNI_EDU_Backend.Application.DTOs.Response;
using UNI_EDU_Backend.Application.Services.Auths;

namespace UNI_EDU_Backend.API.Controllers
{
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            this._authService = authService;
        }

        [HttpPost]
        [Route("api/login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);
            return Ok(new ApiResponse<TokenResponse>
            {
                StatusCode = 200,
                Message = "Login successful",
                Data = response
            });

        }

        [HttpPost]
        [Route("api/register/student")]
        public async Task<IActionResult> RegisterStudentAsync([FromBody] StudentRegister request)
        {
            var response = await _authService.RegisterStudentAsync(request);
            return Created("", new ApiResponse<object>
            {
                StatusCode = 201,
                Message = "Student registered successfully",
                Data = response
            });
        }

        [HttpPost]
        [Route("api/register/tutor")]
        public async Task<IActionResult> RegisterTutorAsync([FromBody] TutorRegister request)
        {
            var response = await _authService.RegisterTutorAsync(request);
            return Created("", new ApiResponse<object>
            {
                StatusCode = 201,
                Message = "Tutor registered successfully",
                Data = response
            });
        }

        [HttpPost]
        [Route("api/refresh-token")]
        public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequest request)
        {
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(new ApiResponse<TokenResponse>
            {
                StatusCode = 200,
                Message = "Refresh token generated successfully",
                Data = response
            });
        }
    }
}