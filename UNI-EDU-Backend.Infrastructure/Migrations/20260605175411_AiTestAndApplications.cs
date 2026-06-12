using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNI_EDU_Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AiTestAndApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiTestAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionsJson = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    Passed = table.Column<bool>(type: "boolean", nullable: false),
                    Used = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiTestAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiTestAttempts_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TutorPostApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorPostApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorPostApplications_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TutorPostApplications_TutorPosts_TutorPostId",
                        column: x => x.TutorPostId,
                        principalTable: "TutorPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiTestAttempts_SubjectId",
                table: "AiTestAttempts",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TutorPostApplications_StudentId",
                table: "TutorPostApplications",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TutorPostApplications_TutorPostId",
                table: "TutorPostApplications",
                column: "TutorPostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiTestAttempts");

            migrationBuilder.DropTable(
                name: "TutorPostApplications");
        }
    }
}
