using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ARABULUCU.DAL.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVehicleListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingFeatures");

            migrationBuilder.DropTable(
                name: "VehicleFeatures");

            migrationBuilder.DropTable(
                name: "VehicleListings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsHiddenFeature = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleFeatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleListings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Mileage = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleListings_UserProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ListingFeatures",
                columns: table => new
                {
                    VehicleListingId = table.Column<int>(type: "integer", nullable: false),
                    VehicleFeatureId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingFeatures", x => new { x.VehicleListingId, x.VehicleFeatureId });
                    table.ForeignKey(
                        name: "FK_ListingFeatures_VehicleFeatures_VehicleFeatureId",
                        column: x => x.VehicleFeatureId,
                        principalTable: "VehicleFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListingFeatures_VehicleListings_VehicleListingId",
                        column: x => x.VehicleListingId,
                        principalTable: "VehicleListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "VehicleFeatures",
                columns: new[] { "Id", "Description", "IsHiddenFeature", "Name" },
                values: new object[,]
                {
                    { 1, "Açılır cam tavan", false, "Sunroof" },
                    { 2, "Ön veya arka koltuk ısıtma sistemi", false, "Isıtmalı Koltuk" },
                    { 3, "LED ön far sistemi", false, "LED Far" },
                    { 4, "Park manevralarını destekleyen kamera", false, "Geri Görüş Kamerası" },
                    { 5, "Gösterge paneli açılış animasyonu", true, "Kadran Selamlama" },
                    { 6, "Araç bilgi-eğlence sistemindeki gizli menü", true, "Gizli Menü Aktivasyonu" },
                    { 7, "Sinyal lambalarının park aydınlatması olarak kullanımı", true, "Amerikan Park" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingFeatures_VehicleFeatureId",
                table: "ListingFeatures",
                column: "VehicleFeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleListings_UserId",
                table: "VehicleListings",
                column: "UserId");
        }
    }
}
