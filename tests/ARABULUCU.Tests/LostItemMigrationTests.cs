using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ARABULUCU.Tests;

public sealed class LostItemMigrationTests
{
    [Fact]
    public void EventDate_UsesLocalTimestampWhileCreatedAtUsesUtcTimestamp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=arabulucu;Username=unused;Password=unused")
            .Options;
        using var db = new ApplicationDbContext(options);

        var lostItem = db.Model.FindEntityType(typeof(LostItem))!;

        Assert.Equal(
            "timestamp without time zone",
            lostItem.FindProperty(nameof(LostItem.EventDate))!.GetColumnType());
        Assert.Equal(
            "timestamp with time zone",
            lostItem.FindProperty(nameof(LostItem.CreatedAt))!.GetColumnType());
    }
}
