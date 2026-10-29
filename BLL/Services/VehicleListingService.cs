using ARABULUCU.BLL.DTOs;
using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;
using Microsoft.EntityFrameworkCore;

namespace ARABULUCU.BLL.Services;

public sealed class VehicleListingService : IVehicleListingService
{
    private readonly ApplicationDbContext _dbContext;

    public VehicleListingService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<VehicleFeature>> GetFeaturesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.VehicleFeatures
            .AsNoTracking()
            .OrderBy(feature => feature.IsHiddenFeature)
            .ThenBy(feature => feature.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<VehicleListingCreateResult> CreateAsync(
        CreateListingDto dto,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.UserProfiles.AnyAsync(profile => profile.Id == userId, cancellationToken))
        {
            return new VehicleListingCreateResult(false, "Hesap profiliniz bulunamadı. Lütfen yeniden giriş yapın.");
        }

        var featureIds = dto.SelectedFeatureIds.Distinct().ToArray();
        var existingFeatureIds = await _dbContext.VehicleFeatures
            .Where(feature => featureIds.Contains(feature.Id))
            .Select(feature => feature.Id)
            .ToListAsync(cancellationToken);

        if (existingFeatureIds.Count != featureIds.Length)
        {
            return new VehicleListingCreateResult(false, "Seçilen özelliklerden biri geçersiz. Lütfen özellikleri yeniden seçin.");
        }

        var listing = new VehicleListing
        {
            Title = dto.Title.Trim(),
            Price = dto.Price,
            Mileage = dto.Mileage,
            Description = dto.Description.Trim(),
            UserId = userId,
            Features = featureIds.Select(featureId => new ListingFeature
            {
                VehicleFeatureId = featureId
            }).ToList()
        };

        _dbContext.VehicleListings.Add(listing);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new VehicleListingCreateResult(true);
    }
}
