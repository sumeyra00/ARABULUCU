using ARABULUCU.BLL.DTOs;
using ARABULUCU.ENT.Entities;

namespace ARABULUCU.BLL.Services;

public interface ILostItemListingService
{
    Task<ItemSearchResult> SearchAsync(ItemSearchCriteria criteria);
    Task<LostItem?> GetByIdAsync(int id);
    Task<IReadOnlyList<RecommendedItem>> GetRecommendationsAsync(LostItem item, int limit = 5);
    Task<bool> CanChatAsync(string userId, int itemId, int matchedItemId);
    Task<string?> GetMatchVerificationQuestionAsync(string userId, int lostItemId, int foundItemId);
    Task<bool> IsMatchVerifiedAsync(int lostItemId, int foundItemId, string userId);
    Task<VerificationResult> VerifyLostItemMatchAsync(string userId, int lostItemId, int foundItemId, string answer);
    Task<IReadOnlyList<ChatMatch>> GetChatMatchesAsync(string userId);
    Task<VerificationResult> VerifyFoundItemAsync(string userId, int foundItemId, string answer);
    Task<bool> CanChatWithVerifiedItemAsync(string userId, int foundItemId, string participantUserId);
    Task<IReadOnlyList<VerifiedChatMatch>> GetVerifiedChatMatchesAsync(string userId);
}
