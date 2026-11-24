namespace ARABULUCU.ENT.Entities;

public sealed class ListingReport
{
    public int Id { get; set; }
    public int LostItemId { get; set; }
    public string ReporterUserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedByUserId { get; set; }
    public string? ResolutionNote { get; set; }
    public LostItem LostItem { get; set; } = null!;
}
