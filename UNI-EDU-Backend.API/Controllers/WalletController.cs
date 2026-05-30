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

    [HttpPost("deposit")]
    [Authorize]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest request, CancellationToken cancellationToken)
    {
        var (userId, _) = ReadCallerOrThrow();

        DepositResponse result = await _walletService.InitiateDepositAsync(request, userId, cancellationToken);

        ApiResponse<DepositResponse> apiResponse = new()
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "Deposit initiated. Redirect the user to payUrl to complete payment.",
            Data = result
        };

        return StatusCode(StatusCodes.Status200OK, apiResponse);
    }

    // Server-to-server callback from Momo. No JWT — trust is established by signature
    // verification inside the service. Returns 204 to acknowledge (stops provider retries).
    [HttpPost("deposit/momo-ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> MomoIpn([FromBody] MomoIpnCallback callback, CancellationToken cancellationToken)
    {
        await _walletService.HandleMomoIpnAsync(callback, cancellationToken);
        return NoContent();
    }

    // VNPay sends the IPN as GET with all data in the query string (including vnp_SecureHash).
    // Trust is established by signature verification inside the service; always reply 200 with
    // VNPay's structured { RspCode, Message } so VNPay stops retrying.
    [HttpGet("deposit/vnpay-ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> VnPayIpn(CancellationToken cancellationToken)
    {
        var providedHash = Request.Query["vnp_SecureHash"].ToString();
        var vnpFields = Request.Query
            .Where(kv => kv.Key.StartsWith("vnp_")
                         && kv.Key != "vnp_SecureHash"
                         && kv.Key != "vnp_SecureHashType")
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

        var response = await _walletService.HandleVnPayIpnAsync(vnpFields, providedHash, cancellationToken);
        return Ok(response);
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
