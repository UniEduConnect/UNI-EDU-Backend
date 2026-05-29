using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNI_EDU_Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletTxStatusAndPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Method",
                table: "WalletTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderRef",
                table: "WalletTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderTxnId",
                table: "WalletTransactions",
                type: "text",
                nullable: true);

            // Default 1 = WalletTxStatus.Completed: existing rows are historical settled entries,
            // and internal ledger writes (escrow/refund) are created already-completed. Deposits
            // explicitly set Pending in code.
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "WalletTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Method",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "ProviderRef",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "ProviderTxnId",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "WalletTransactions");
        }
    }
}
