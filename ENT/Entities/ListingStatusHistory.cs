namespace ARABULUCU.ENT.Entities;

public sealed class ListingStatusHistory
{
    public int Id { get; set; }
    public int LostItemId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ChangedByUserId { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public LostItem LostItem { get; set; } = null!;
}
