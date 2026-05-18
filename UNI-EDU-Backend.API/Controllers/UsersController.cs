using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.DTOs.SendOTP;
using UNI_EDU_Backend.Application.Services.Users;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    [HttpPost("check-phone")]
    public async Task<IActionResult> CheckPhoneNumber([FromBody] CheckPhoneUserRequest sendOTPUserRequest)
    {
        CheckPhoneUserResponse response = await _userService.CheckPhoneNumberAsync(sendOTPUserRequest);

        ApiResponse<CheckPhoneUserResponse> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Check phone number successfully",
            Data = response
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }
}
