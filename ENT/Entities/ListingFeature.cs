namespace ARABULUCU.ENT.Entities;

public class ListingFeature
{
    public int VehicleListingId { get; set; }
    public VehicleListing VehicleListing { get; set; } = null!;
    public int VehicleFeatureId { get; set; }
    public VehicleFeature VehicleFeature { get; set; } = null!;
}
