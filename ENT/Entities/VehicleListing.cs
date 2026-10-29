namespace ARABULUCU.ENT.Entities;

public class VehicleListing
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Mileage { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = string.Empty;
    public UserProfile User { get; set; } = null!;
    public ICollection<ListingFeature> Features { get; set; } = new List<ListingFeature>();
}
