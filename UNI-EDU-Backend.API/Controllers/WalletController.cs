using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Wallets;
using UNI_EDU_Backend.Application.Services.Wallets;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WalletController(IWalletService walletService) : ControllerBase
{
    private readonly IWalletService _walletService = walletService;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyWallet(CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        WalletResponse result = await _walletService.GetMyWalletAsync(userId, role, cancellationToken);

        ApiResponse<WalletResponse> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get wallet successfully",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    [HttpGet("transactions")]
    [Authorize]
    public async Task<IActionResult> GetMyTransactions([FromQuery] TransactionListQuery query, CancellationToken cancellationToken)
    {
        var (userId, role) = ReadCallerOrThrow();

        PagedResult<TransactionResponse> result = await _walletService.GetMyTransactionsAsync(query, userId, role, cancellationToken);

        ApiResponse<PagedResult<TransactionResponse>> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get transactions successfully",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    private (Guid UserId, string Role) ReadCallerOrThrow()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user identifier claim.");

        var role = User.FindFirst(ClaimTypes.Role)?.Value
            ?? throw new UnauthorizedAccessException("Missing role claim.");

        return (userId, role);
    }
}
