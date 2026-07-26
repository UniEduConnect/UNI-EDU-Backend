using Microsoft.EntityFrameworkCore;
using UNI_EDU_Backend.Application.Exceptions;
using UNI_EDU_Backend.Domain.Enums;
using UNI_EDU_Backend.Domain.Models;

namespace UNI_EDU_Backend.Infrastructure.Common;

/// <summary>
/// Charges a class's full tuition (per-session fee × total sessions) from the student's wallet
/// into escrow at class-creation time. Shared by the request/post acceptance flows so a class is
/// only ever created once the student (or the parent, via the child's wallet) has actually paid.
/// The escrow is released to the tutor session-by-session (see SessionRepository).
/// </summary>
public static class ClassEscrow
{
    /// <summary>Total tuition for a class = per-session fee × number of sessions.</summary>
    public static decimal TotalTuition(decimal perSessionFee, int totalSessions) =>
        perSessionFee > 0 && totalSessions > 0 ? perSessionFee * totalSessions : 0m;

    /// <summary>
    /// Debits <paramref name="total"/> from the student's spendable balance into their escrow
    /// balance and records an EscrowIn ledger row (all pending on the caller's SaveChanges).
    /// Throws <see cref="BadRequestException"/> if the wallet is missing or underfunded. A zero /
    /// negative total is a no-op (an unpriced class charges nothing).
    /// </summary>
    public static async Task ChargeStudentAsync(
        ApplicationDbContext db,
        Guid studentId,
        Guid classId,
        string className,
        decimal total,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (total <= 0m) return;

        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserID == studentId, cancellationToken)
            ?? throw new BadRequestException("Ví học sinh chưa được khởi tạo. Vui lòng nạp tiền trước khi bắt đầu lớp.");

        if (wallet.Balance < total)
            throw new BadRequestException(
                $"Số dư ví không đủ để bắt đầu lớp. Cần {total:N0}đ, hiện có {wallet.Balance:N0}đ. Vui lòng nạp thêm.");

        wallet.Balance -= total;
        wallet.EscrowBalance += total;
        wallet.UpdatedAt = now;

        db.WalletTransactions.Add(new WalletTransaction
        {
            TransactionID = Guid.NewGuid(),
            UserID = studentId,
            Type = WalletTxType.EscrowIn,
            Amount = total,
            RelatedClassID = classId,
            Description = $"Ký quỹ học phí lớp {className}",
            CreatedAt = now
        });
    }

    /// <summary>
    /// Read-only sibling of <see cref="ChargeStudentAsync"/>: verifies the student's wallet could
    /// cover <paramref name="total"/> WITHOUT touching it (no debit, no ledger row) — used by the
    /// "can this class be accepted?" pre-check that runs before a tutor takes the AI test. Mirrors
    /// ChargeStudentAsync's guards exactly: a zero / negative total is a no-op, a missing wallet or an
    /// underfunded balance throws <see cref="BadRequestException"/>.
    /// </summary>
    public static async Task EnsureAffordableAsync(
        ApplicationDbContext db,
        Guid studentId,
        decimal total,
        CancellationToken cancellationToken)
    {
        if (total <= 0m) return;

        var wallet = await db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserID == studentId, cancellationToken)
            ?? throw new BadRequestException("Ví học sinh chưa được khởi tạo. Học sinh cần nạp tiền trước khi bắt đầu lớp.");

        if (wallet.Balance < total)
            throw new BadRequestException(
                $"Số dư ví học sinh không đủ để bắt đầu lớp (cần {total:N0}đ). Học sinh cần nạp thêm.");
    }
}
