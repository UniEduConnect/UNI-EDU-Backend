using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UNI_EDU_Backend.API.Commons;
using UNI_EDU_Backend.Application.Commons;
using UNI_EDU_Backend.Application.DTOs.Wallets;
using UNI_EDU_Backend.Application.DTOs.Withdrawals;
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

    // Server-to-server webhook from PayOS. No JWT — trust via signature verification in the service.
    [HttpPost("deposit/payos-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PayOsWebhook([FromBody] PayOsWebhookPayload payload, CancellationToken cancellationToken)
    {
        await _walletService.HandlePayOsWebhookAsync(payload, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new { success = true });
    }

    [HttpPost("withdraw")]
    [Authorize(Roles = "Tutor")]
    public async Task<IActionResult> Withdraw([FromBody] CreateWithdrawalRequest request, CancellationToken cancellationToken)
    {
        var (userId, _) = ReadCallerOrThrow();

        WithdrawalResponse result = await _walletService.CreateWithdrawalAsync(request, userId, cancellationToken);

        ApiResponse<WithdrawalResponse> apiResponse = new()
        {
            StatusCode = StatusCodes.Status201Created,
            Message = "Withdrawal request created. Awaiting finance approval.",
            Data = result
        };

        return StatusCode(StatusCodes.Status201Created, apiResponse);
    }

    // VNPay sends the IPN as GET with all data in the query string (including vnp_SecureHash).
    // Trust is established by signature verification inside the service; always reply 200 with
    // VNPay's structured { RspCode, Message } so VNPay stops retrying.
    [HttpGet("deposit/vnpay-ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> VnPayIpn(CancellationToken cancellationToken)
    {
        var (vnpFields, providedHash) = ReadRawVnpFields();
        var response = await _walletService.HandleVnPayIpnAsync(vnpFields, providedHash, cancellationToken);
        return Ok(response);
    }

    // Browser-return confirm: the FE return page forwards VNPay's signed query params here so the
    // wallet credits immediately (verified by signature, idempotent) without waiting on the IPN.
    [HttpGet("deposit/vnpay-return")]
    [AllowAnonymous]
    public async Task<IActionResult> VnPayReturn(CancellationToken cancellationToken)
    {
        var (vnpFields, providedHash) = ReadRawVnpFields();
        VnPayReturnResult result = await _walletService.HandleVnPayReturnAsync(vnpFields, providedHash, cancellationToken);
        return StatusCode(StatusCodes.Status200OK, new ApiResponse<VnPayReturnResult>
        {
            StatusCode = StatusCodes.Status200OK,
            Message = "OK",
            Data = result,
        });
    }

    // Parse the RAW query string (values stay URL-encoded exactly as VNPay sent them) so the
    // signature is verified over the same bytes VNPay signed — Request.Query would decode them.
    private (Dictionary<string, string> Fields, string Hash) ReadRawVnpFields()
    {
        var raw = Request.QueryString.Value ?? string.Empty;
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        var hash = string.Empty;

        foreach (var pair in raw.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = pair.IndexOf('=');
            var key = i < 0 ? pair : pair[..i];
            var val = i < 0 ? string.Empty : pair[(i + 1)..];

            if (string.Equals(key, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase)) { hash = val; continue; }
            if (string.Equals(key, "vnp_SecureHashType", StringComparison.OrdinalIgnoreCase)) continue;
            if (key.StartsWith("vnp_", StringComparison.Ordinal)) fields[key] = val;
        }
        return (fields, hash);
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
