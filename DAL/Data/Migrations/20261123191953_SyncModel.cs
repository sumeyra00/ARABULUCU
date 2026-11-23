using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARABULUCU.DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerifiedItemMatches_FoundItemId_UserId",
                table: "VerifiedItemMatches");

            migrationBuilder.AddColumn<int>(
                name: "LostItemId",
                table: "VerifiedItemMatches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedItemMatches_FoundItemId_LostItemId_UserId",
                table: "VerifiedItemMatches",
                columns: new[] { "FoundItemId", "LostItemId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedItemMatches_LostItemId",
                table: "VerifiedItemMatches",
                column: "LostItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_VerifiedItemMatches_LostItems_LostItemId",
                table: "VerifiedItemMatches",
                column: "LostItemId",
                principalTable: "LostItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VerifiedItemMatches_LostItems_LostItemId",
                table: "VerifiedItemMatches");

            migrationBuilder.DropIndex(
                name: "IX_VerifiedItemMatches_FoundItemId_LostItemId_UserId",
                table: "VerifiedItemMatches");

            migrationBuilder.DropIndex(
                name: "IX_VerifiedItemMatches_LostItemId",
                table: "VerifiedItemMatches");

            migrationBuilder.DropColumn(
                name: "LostItemId",
                table: "VerifiedItemMatches");

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedItemMatches_FoundItemId_UserId",
                table: "VerifiedItemMatches",
                columns: new[] { "FoundItemId", "UserId" },
                unique: true);
        }
    }
}
