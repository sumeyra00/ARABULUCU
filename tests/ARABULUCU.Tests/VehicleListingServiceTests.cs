using System.Linq;
using System.Threading.Tasks;
using ARABULUCU.BLL.DTOs;
using ARABULUCU.BLL.Services;
using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ARABULUCU.Tests;

public class VehicleListingServiceTests
{
    [Fact]
    public async Task CreateAsync_SavesListingWithSelectedFeatures()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.UserProfiles.Add(new UserProfile { Id = "seller", DisplayName = "Seller", Email = "seller@example.com" });
        await dbContext.SaveChangesAsync();

        var service = new VehicleListingService(dbContext);
        var result = await service.CreateAsync(new CreateListingDto
        {
            Title = "Bakımlı sedan",
            Price = 750000,
            Mileage = 82000,
            Description = "Düzenli bakımlı.",
            SelectedFeatureIds = [1, 5, 1]
        }, "seller");

        Assert.True(result.Succeeded);
        var listing = await dbContext.VehicleListings.SingleAsync();
        Assert.Equal("Bakımlı sedan", listing.Title);
        Assert.Equal(750000, listing.Price);
        Assert.Equal(82000, listing.Mileage);
        Assert.Equal(
            new[] { 1, 5 },
            await dbContext.ListingFeatures
                .Where(feature => feature.VehicleListingId == listing.Id)
                .OrderBy(feature => feature.VehicleFeatureId)
                .Select(feature => feature.VehicleFeatureId)
                .ToArrayAsync());
    }

    [Fact]
    public async Task CreateAsync_RejectsUnknownFeatureIdsWithoutSavingListing()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.UserProfiles.Add(new UserProfile { Id = "seller", DisplayName = "Seller", Email = "seller@example.com" });
        await dbContext.SaveChangesAsync();

        var service = new VehicleListingService(dbContext);
        var result = await service.CreateAsync(new CreateListingDto
        {
            Title = "Bakımlı sedan",
            Price = 750000,
            Mileage = 82000,
            Description = "Düzenli bakımlı.",
            SelectedFeatureIds = [1, 999]
        }, "seller");

        Assert.False(result.Succeeded);
        Assert.NotNull(result.ErrorMessage);
        Assert.Empty(await dbContext.VehicleListings.ToListAsync());
        Assert.Empty(await dbContext.ListingFeatures.ToListAsync());
    }
}
