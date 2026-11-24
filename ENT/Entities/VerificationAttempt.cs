namespace ARABULUCU.ENT.Entities;

public sealed class VerificationAttempt
{
    public int Id { get; set; }
    public int LostItemId { get; set; }
    public string ClaimerUserId { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime LastAttemptAt { get; set; } = DateTime.UtcNow;
    public LostItem LostItem { get; set; } = null!;
}
