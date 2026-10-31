using ARABULUCU.ENT.Entities;

namespace ARABULUCU.BLL.DTOs;

public sealed class ChatMatch
{
    public required LostItem Item { get; init; }
    public required LostItem MatchedItem { get; init; }
    public decimal SimilarityPercent { get; init; }
}
