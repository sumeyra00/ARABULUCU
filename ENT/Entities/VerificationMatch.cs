namespace ARABULUCU.ENT.Entities;

public class VerificationMatch
{
    public int Id { get; set; }
    public int LostItemId { get; set; }
    public string ClaimerUserId { get; set; } = string.Empty;
    public int CorrectAnswerCount { get; set; }
    public int Score { get; set; }
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;

    public LostItem LostItem { get; set; } = null!;
}
