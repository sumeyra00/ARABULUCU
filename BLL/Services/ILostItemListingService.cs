using ARABULUCU.BLL.DTOs;
using ARABULUCU.ENT.Entities;

namespace ARABULUCU.BLL.Services;

public interface ILostItemListingService
{
    Task<ItemSearchResult> SearchAsync(ItemSearchCriteria criteria);
    Task<LostItem?> GetByIdAsync(int id);
    Task<IReadOnlyList<RecommendedItem>> GetRecommendationsAsync(LostItem item, int limit = 5);
    Task<bool> CanChatAsync(string userId, int itemId, int matchedItemId);
    Task<IReadOnlyList<ChatMatch>> GetChatMatchesAsync(string userId);
    Task<IReadOnlyList<VerificationQuestionDto>> GetVerificationQuestionsAsync(int itemId, bool includeSecretQuestion = true);
    Task<IReadOnlyList<VerificationQuestionDto>> GetMatchingQuestionsAsync();
    Task<MatchingResult> FindMatchesAsync(MatchingCriteria criteria);
    Task<VerificationResult> VerifyAsync(int itemId, string claimerUserId, IReadOnlyCollection<VerificationAnswerDto> answers);
    Task<ProtectedListingDto?> GetProtectedDetailsAsync(int itemId, string claimerUserId);
}
