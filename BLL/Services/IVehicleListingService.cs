using ARABULUCU.BLL.DTOs;
using ARABULUCU.ENT.Entities;

namespace ARABULUCU.BLL.Services;

public interface IVehicleListingService
{
    Task<IReadOnlyList<VehicleFeature>> GetFeaturesAsync(CancellationToken cancellationToken = default);
    Task<VehicleListingCreateResult> CreateAsync(
        CreateListingDto dto,
        string userId,
        CancellationToken cancellationToken = default);
}

public sealed record VehicleListingCreateResult(bool Succeeded, string? ErrorMessage = null);
