using ARABULUCU.BLL.DTOs;
using ARABULUCU.ENT.Entities;

namespace ARABULUCU.BLL.Services;

public interface ILostItemListingService
{
    Task<ItemSearchResult> SearchAsync(ItemSearchCriteria criteria);
    Task<LostItem?> GetByIdAsync(int id);
    Task<IReadOnlyList<RecommendedItem>> GetRecommendationsAsync(LostItem item, int limit = 5);
}
