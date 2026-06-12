using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNI_EDU_Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExamConfigQuestionMetaAndSubmissionScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AIFeedback",
                table: "Submissions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "CorrectCount",
                table: "Submissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalQuestions",
                table: "Submissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Standard",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AiProctoring",
                table: "Exams",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Exams",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "Exams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Exams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Fee",
                table: "Exams",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttemptsPerUser",
                table: "Exams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScoreScale",
                table: "Exams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Exams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Exams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Exams",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectCount",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "TotalQuestions",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Standard",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "AiProctoring",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "Fee",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "MaxAttemptsPerUser",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "ScoreScale",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Exams");

            migrationBuilder.AlterColumn<string>(
                name: "AIFeedback",
                table: "Submissions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
