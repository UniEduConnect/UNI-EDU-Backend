using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Wallets;
using UNI_EDU_Backend.Application.DTOs.Withdrawals;
using UNI_EDU_Backend.Application.Services.Finance;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Finance")] // Finance portal: Admin or Finance staff.
public class FinanceController(IFinanceService financeService) : ControllerBase
{
    private readonly IFinanceService _financeService = financeService;

    [HttpGet("withdrawals")]
    public async Task<IActionResult> GetWithdrawals([FromQuery] WithdrawalListQuery query, CancellationToken cancellationToken)
    {
        PagedResult<WithdrawalAdminResponse> result = await _financeService.GetWithdrawalsAsync(query, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<WithdrawalAdminResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get withdrawals successfully",
            Data = result
        });
    }

    [HttpPost("withdrawals/{id:guid}/approve")]
    public async Task<IActionResult> ApproveWithdrawal(Guid id, [FromBody] ReviewWithdrawalRequest request, CancellationToken cancellationToken)
    {
        var reviewerId = ReadCallerIdOrThrow();
        await _financeService.ApproveWithdrawalAsync(id, request ?? new ReviewWithdrawalRequest(), reviewerId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Withdrawal approved successfully",
            Data = null
        });
    }

    [HttpPost("withdrawals/{id:guid}/reject")]
    public async Task<IActionResult> RejectWithdrawal(Guid id, [FromBody] ReviewWithdrawalRequest request, CancellationToken cancellationToken)
    {
        var reviewerId = ReadCallerIdOrThrow();
        await _financeService.RejectWithdrawalAsync(id, request ?? new ReviewWithdrawalRequest(), reviewerId, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<object>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Withdrawal rejected successfully",
            Data = null
        });
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] AdminTransactionListQuery query, CancellationToken cancellationToken)
    {
        PagedResult<AdminTransactionResponse> result = await _financeService.GetTransactionsAsync(query, cancellationToken);

        return StatusCode(StatusCodes.Status200OK, new ApiResponse<PagedResult<AdminTransactionResponse>>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Get transactions successfully",
            Data = result
        });
    }

    private Guid ReadCallerIdOrThrow()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user identifier claim.");
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user identifier claim.");
        return userId;
    }
}
