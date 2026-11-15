using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ARABULUCU.DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationAndHiddenProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnswerHash",
                table: "LostItemFeatures",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerificationQuestion",
                table: "LostItemFeatures",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Weight",
                table: "LostItemFeatures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "VerificationMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LostItemId = table.Column<int>(type: "integer", nullable: false),
                    ClaimerUserId = table.Column<string>(type: "text", nullable: false),
                    CorrectAnswerCount = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationMatches_LostItems_LostItemId",
                        column: x => x.LostItemId,
                        principalTable: "LostItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerificationMatches_LostItemId_ClaimerUserId",
                table: "VerificationMatches",
                columns: new[] { "LostItemId", "ClaimerUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerificationMatches");

            migrationBuilder.DropColumn(
                name: "AnswerHash",
                table: "LostItemFeatures");

            migrationBuilder.DropColumn(
                name: "IsVerificationQuestion",
                table: "LostItemFeatures");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "LostItemFeatures");
        }
    }
}
