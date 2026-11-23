namespace ARABULUCU.ENT.Entities;

public sealed class VerifiedItemMatch
{
    public int Id { get; set; }
    public int FoundItemId { get; set; }
    public LostItem FoundItem { get; set; } = null!;
    public int? LostItemId { get; set; }
    public LostItem? LostItem { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
}
