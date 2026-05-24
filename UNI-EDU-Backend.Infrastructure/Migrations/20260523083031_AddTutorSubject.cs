using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNI_EDU_Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "Achievements",
                table: "Tutors",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]");

            migrationBuilder.AddColumn<string>(
                name: "AvailableSlots",
                table: "Tutors",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Tutors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<List<string>>(
                name: "Certificates",
                table: "Tutors",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]");

            migrationBuilder.AddColumn<int>(
                name: "HourlyRate",
                table: "Tutors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IntroVideoUrl",
                table: "Tutors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Tutors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Tutors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "School",
                table: "Tutors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TeachingStyle",
                table: "Tutors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TutorType",
                table: "Tutors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "YearsExperience",
                table: "Tutors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TutorSubjects",
                columns: table => new
                {
                    TutorID = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectID = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorSubjects", x => new { x.TutorID, x.SubjectID });
                    table.ForeignKey(
                        name: "FK_TutorSubjects_Subjects_SubjectID",
                        column: x => x.SubjectID,
                        principalTable: "Subjects",
                        principalColumn: "SubjectID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TutorSubjects_Tutors_TutorID",
                        column: x => x.TutorID,
                        principalTable: "Tutors",
                        principalColumn: "TutorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TutorSubjects_SubjectID",
                table: "TutorSubjects",
                column: "SubjectID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TutorSubjects");

            migrationBuilder.DropColumn(
                name: "Achievements",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "AvailableSlots",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "Certificates",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "IntroVideoUrl",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "School",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "TeachingStyle",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "TutorType",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "YearsExperience",
                table: "Tutors");
        }
    }
}
