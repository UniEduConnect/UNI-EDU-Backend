using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNI_EDU_Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWithdrawals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Withdrawals",
                columns: table => new
                {
                    WithdrawalID = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorID = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Method = table.Column<string>(type: "text", nullable: false),
                    BankName = table.Column<string>(type: "text", nullable: true),
                    BankAccount = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewerID = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewNote = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Withdrawals", x => x.WithdrawalID);
                    table.ForeignKey(
                        name: "FK_Withdrawals_Tutors_TutorID",
                        column: x => x.TutorID,
                        principalTable: "Tutors",
                        principalColumn: "TutorID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Withdrawals_Users_ReviewerID",
                        column: x => x.ReviewerID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Withdrawals_ReviewerID",
                table: "Withdrawals",
                column: "ReviewerID");

            migrationBuilder.CreateIndex(
                name: "IX_Withdrawals_TutorID",
                table: "Withdrawals",
                column: "TutorID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Withdrawals");
        }
    }
}
