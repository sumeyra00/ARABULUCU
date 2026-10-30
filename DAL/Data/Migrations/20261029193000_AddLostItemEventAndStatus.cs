using ARABULUCU.DAL.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARABULUCU.DAL.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261029193000_AddLostItemEventAndStatus")]
public partial class AddLostItemEventAndStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "EventDate",
            table: "LostItems",
            type: "TEXT",
            nullable: false,
            defaultValue: new DateTime(1970, 1, 1));

        migrationBuilder.AddColumn<bool>(
            name: "IsItemFound",
            table: "LostItems",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "IsDelivered",
            table: "LostItems",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql(
            """
            CREATE TABLE "__LostItemFeatures_new" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_LostItemFeatures" PRIMARY KEY AUTOINCREMENT,
                "LostItemId" INTEGER NOT NULL,
                "Key" TEXT NOT NULL,
                "Value" TEXT NOT NULL,
                "IsHidden" INTEGER NOT NULL,
                CONSTRAINT "FK_LostItemFeatures_LostItems_LostItemId"
                    FOREIGN KEY ("LostItemId") REFERENCES "LostItems" ("Id") ON DELETE CASCADE
            );
            INSERT INTO "__LostItemFeatures_new" ("Id", "LostItemId", "Key", "Value", "IsHidden")
                SELECT "Id", "LostItemId", "Key", "Value", "IsHidden" FROM "LostItemFeatures";
            DROP TABLE "LostItemFeatures";
            ALTER TABLE "__LostItemFeatures_new" RENAME TO "LostItemFeatures";
            """);

        migrationBuilder.CreateIndex(
            name: "IX_LostItemFeatures_LostItemId",
            table: "LostItemFeatures",
            column: "LostItemId");

        migrationBuilder.Sql(
            """
            UPDATE "LostItems"
            SET "EventDate" = "CreatedAt",
                "IsItemFound" = "IsFound",
                "IsDelivered" = CASE WHEN EXISTS (
                    SELECT 1 FROM "LostItemFeatures"
                    WHERE "LostItemFeatures"."LostItemId" = "LostItems"."Id"
                      AND "LostItemFeatures"."Key" = 'ItemStatus'
                      AND "LostItemFeatures"."Value" = 'delivered'
                ) THEN 1 ELSE 0 END,
                "CreatedAt" = CURRENT_TIMESTAMP
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_LostItemFeatures_LostItemId",
            table: "LostItemFeatures");

        migrationBuilder.Sql(
            """
            CREATE TABLE "__LostItemFeatures_old" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_LostItemFeatures" PRIMARY KEY AUTOINCREMENT,
                "LostItemId" INTEGER NOT NULL,
                "Key" TEXT NOT NULL,
                "Value" TEXT NOT NULL,
                "IsHidden" INTEGER NOT NULL
            );
            INSERT INTO "__LostItemFeatures_old" ("Id", "LostItemId", "Key", "Value", "IsHidden")
                SELECT "Id", "LostItemId", "Key", "Value", "IsHidden" FROM "LostItemFeatures";
            DROP TABLE "LostItemFeatures";
            ALTER TABLE "__LostItemFeatures_old" RENAME TO "LostItemFeatures";
            """);

        migrationBuilder.DropColumn(name: "EventDate", table: "LostItems");
        migrationBuilder.DropColumn(name: "IsItemFound", table: "LostItems");
        migrationBuilder.DropColumn(name: "IsDelivered", table: "LostItems");
    }
}
