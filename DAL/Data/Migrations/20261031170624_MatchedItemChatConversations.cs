using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARABULUCU.DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class MatchedItemChatConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MatchedLostItemId",
                table: "ContactMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderUserId",
                table: "ContactMessages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchedLostItemId",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "SenderUserId",
                table: "ContactMessages");
        }
    }
}
