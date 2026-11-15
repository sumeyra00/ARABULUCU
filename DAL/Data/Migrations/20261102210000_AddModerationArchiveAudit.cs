using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ARABULUCU.DAL.Data.Migrations;

[Migration("20261102210000_AddModerationArchiveAudit")]
public partial class AddModerationArchiveAudit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>("IsArchived", "LostItems", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTime>("LastActivityAt", "LostItems", type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP");

        migrationBuilder.CreateTable(
            name: "VerificationAttempts",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                LostItemId = table.Column<int>(type: "integer", nullable: false),
                ClaimerUserId = table.Column<string>(type: "text", nullable: false),
                FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                LockedUntil = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                LastAttemptAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VerificationAttempts", x => x.Id);
                table.ForeignKey("FK_VerificationAttempts_LostItems_LostItemId", x => x.LostItemId, "LostItems", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ListingReports",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                LostItemId = table.Column<int>(type: "integer", nullable: false),
                ReporterUserId = table.Column<string>(type: "text", nullable: false),
                Reason = table.Column<string>(type: "text", nullable: false),
                Details = table.Column<string>(type: "text", nullable: true),
                Status = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                ReviewedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                ReviewedByUserId = table.Column<string>(type: "text", nullable: true),
                ResolutionNote = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ListingReports", x => x.Id);
                table.ForeignKey("FK_ListingReports_LostItems_LostItemId", x => x.LostItemId, "LostItems", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ListingStatusHistories",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                LostItemId = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                ChangedByUserId = table.Column<string>(type: "text", nullable: true),
                Details = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ListingStatusHistories", x => x.Id);
                table.ForeignKey("FK_ListingStatusHistories_LostItems_LostItemId", x => x.LostItemId, "LostItems", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_VerificationAttempts_LostItemId_ClaimerUserId", "VerificationAttempts", new[] { "LostItemId", "ClaimerUserId" }, unique: true);
        migrationBuilder.CreateIndex("IX_ListingReports_LostItemId", "ListingReports", "LostItemId");
        migrationBuilder.CreateIndex("IX_ListingStatusHistories_LostItemId", "ListingStatusHistories", "LostItemId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ListingStatusHistories");
        migrationBuilder.DropTable("ListingReports");
        migrationBuilder.DropTable("VerificationAttempts");
        migrationBuilder.DropColumn("IsArchived", "LostItems");
        migrationBuilder.DropColumn("LastActivityAt", "LostItems");
    }
}
