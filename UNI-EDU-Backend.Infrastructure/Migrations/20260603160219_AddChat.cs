using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNI_EDU_Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    MessageID = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassID = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderID = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.MessageID);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Classes_ClassID",
                        column: x => x.ClassID,
                        principalTable: "Classes",
                        principalColumn: "ClassID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Users_SenderID",
                        column: x => x.SenderID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassChatReads",
                columns: table => new
                {
                    ClassID = table.Column<Guid>(type: "uuid", nullable: false),
                    UserID = table.Column<Guid>(type: "uuid", nullable: false),
                    LastReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassChatReads", x => new { x.ClassID, x.UserID });
                });

            migrationBuilder.CreateTable(
                name: "DmMessages",
                columns: table => new
                {
                    MessageID = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentID = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorID = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderID = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DmMessages", x => x.MessageID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ClassID_SentAt",
                table: "ChatMessages",
                columns: new[] { "ClassID", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderID",
                table: "ChatMessages",
                column: "SenderID");

            migrationBuilder.CreateIndex(
                name: "IX_DmMessages_TutorID_ParentID_SentAt",
                table: "DmMessages",
                columns: new[] { "TutorID", "ParentID", "SentAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ClassChatReads");

            migrationBuilder.DropTable(
                name: "DmMessages");
        }
    }
}
