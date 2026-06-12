using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNI_EDU_Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentsAndExamAiConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    AppointmentID = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    WithName = table.Column<string>(type: "text", nullable: true),
                    WithUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.AppointmentID);
                    table.ForeignKey(
                        name: "FK_Appointments_Users_WithUserId",
                        column: x => x.WithUserId,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ExamAiConfigs",
                columns: table => new
                {
                    ConfigID = table.Column<Guid>(type: "uuid", nullable: false),
                    ProctoringEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    FaceDetection = table.Column<bool>(type: "boolean", nullable: false),
                    FullscreenRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CopyPasteBlocked = table.Column<bool>(type: "boolean", nullable: false),
                    TabSwitchLimit = table.Column<int>(type: "integer", nullable: false),
                    AutoGenerateEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultDifficulty = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAiConfigs", x => x.ConfigID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_WithUserId",
                table: "Appointments",
                column: "WithUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "ExamAiConfigs");
        }
    }
}
