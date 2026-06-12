namespace UNI_EDU_Backend.Domain.Enums
{
    public enum WalletTxType
    {
        Deposit = 0,
        EscrowIn = 1,
        EscrowRelease = 2,
        Withdrawal = 3,
        Refund = 4,
        PlatformFee = 5,
        TransferIn = 6,   // money received into this wallet (e.g. from a parent)
        TransferOut = 7   // money sent out of this wallet (e.g. parent funding a child)
    }
}
