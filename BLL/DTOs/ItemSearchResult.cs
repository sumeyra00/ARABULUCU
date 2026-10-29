using ARABULUCU.ENT.Entities;

namespace ARABULUCU.BLL.DTOs;

public sealed class ItemSearchResult
{
    public IReadOnlyList<LostItem> Items { get; init; } = [];
    public string? ValidationError { get; init; }
}

public sealed class RecommendedItem
{
    public required LostItem Item { get; init; }
    public decimal SimilarityPercent { get; init; }
}
