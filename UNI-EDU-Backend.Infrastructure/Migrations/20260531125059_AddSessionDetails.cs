using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNI_EDU_Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AbsenceApproved",
                table: "Sessions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AbsenceReason",
                table: "Sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AbsenceRequestedBy",
                table: "Sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "Sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Homework",
                table: "Sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "HomeworkFiles",
                table: "Sessions",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RatingComment",
                table: "Sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "Sessions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbsenceApproved",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "AbsenceReason",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "AbsenceRequestedBy",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Homework",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "HomeworkFiles",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "RatingComment",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "Sessions");
        }
    }
}
