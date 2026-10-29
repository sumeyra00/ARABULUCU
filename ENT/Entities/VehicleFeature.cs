namespace ARABULUCU.ENT.Entities;

public class VehicleFeature
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsHiddenFeature { get; set; }
    public ICollection<ListingFeature> Listings { get; set; } = new List<ListingFeature>();
}
