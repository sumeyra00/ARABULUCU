using System;
using System.Threading.Tasks;
using ARABULUCU.DAL.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ARABULUCU.Tests;

public sealed class LostItemMigrationTests
{
    [Fact]
    public async Task Migration_PreservesLegacyEventDateAndMapsIndependentStatuses()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApplicationDbContext(options);

        await db.Database.MigrateAsync("20261029133317_AddVehicleListingsAndFeatures");
        await db.Database.ExecuteSqlRawAsync(
            """INSERT INTO "UserProfiles" ("Id", "DisplayName", "Email") VALUES ('owner', 'Owner', 'owner@example.com')""");
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "LostItems"
                ("Title", "Description", "Location", "CreatedAt", "IsFound", "UserId", "CategoryId")
            VALUES
                ('Legacy found item', 'Description', 'Library', '2026-10-01 10:00:00', 1, 'owner', 1)
            """);
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "LostItemFeatures" ("LostItemId", "Key", "Value", "IsHidden")
            VALUES (1, 'ItemStatus', 'delivered', 1)
            """);

        await db.Database.MigrateAsync();

        var migrated = await db.LostItems.AsNoTracking().SingleAsync();
        Assert.Equal(new DateTime(2026, 10, 1, 10, 0, 0), migrated.EventDate);
        Assert.True(migrated.IsFound);
        Assert.True(migrated.IsItemFound);
        Assert.True(migrated.IsDelivered);
        Assert.NotEqual(migrated.EventDate, migrated.CreatedAt);
    }
}
