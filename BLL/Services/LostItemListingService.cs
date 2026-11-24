using ARABULUCU.BLL.DTOs;
using ARABULUCU.DAL.Data;
using ARABULUCU.DAL.Repositories;
using ARABULUCU.ENT.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ARABULUCU.BLL.Services;

public sealed class LostItemListingService : ILostItemListingService
{
    private const decimal MinimumRecommendationScore = 50;
    private const decimal MinimumChatMatchScore = 50;

    private readonly ApplicationDbContext _dbContext;
    private readonly IItemRepository _itemRepository;
    private readonly ILostItemSimilarityService _similarityService;

    public LostItemListingService(
        ApplicationDbContext dbContext,
        IItemRepository itemRepository,
        ILostItemSimilarityService similarityService)
    {
        _dbContext = dbContext;
        _itemRepository = itemRepository;
        _similarityService = similarityService;
    }

    public async Task<ItemSearchResult> SearchAsync(ItemSearchCriteria criteria)
    {
        if (criteria.EventDateFrom.HasValue && criteria.EventDateTo.HasValue &&
            criteria.EventDateFrom.Value.Date > criteria.EventDateTo.Value.Date)
        {
            return new ItemSearchResult { ValidationError = "Başlangıç tarihi bitiş tarihinden sonra olamaz." };
        }

        IQueryable<LostItem> items = _itemRepository.Query()
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Features.Where(feature => !feature.IsHidden));

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var query = Normalize(criteria.Query);
            items = items.Where(item =>
                item.Title.ToLower().Contains(query) ||
                item.Description.ToLower().Contains(query));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Category))
        {
            var category = Normalize(criteria.Category);
            items = items.Where(item => item.Category.Name.ToLower() == category);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Location))
        {
            var location = Normalize(criteria.Location);
            items = items.Where(item => item.Location.ToLower().Contains(location));
        }

        if (criteria.IsFound.HasValue)
        {
            items = items.Where(item => item.IsFound == criteria.IsFound.Value);
        }

        if (criteria.EventDateFrom.HasValue)
        {
            var from = criteria.EventDateFrom.Value.Date;
            items = items.Where(item => item.EventDate.Date >= from);
        }

        if (criteria.EventDateTo.HasValue)
        {
            var to = criteria.EventDateTo.Value.Date;
            items = items.Where(item => item.EventDate.Date <= to);
        }

        if (criteria.IsItemFound.HasValue)
        {
            items = items.Where(item => item.IsItemFound == criteria.IsItemFound.Value);
        }

        if (criteria.IsDelivered.HasValue)
        {
            items = items.Where(item => item.IsDelivered == criteria.IsDelivered.Value);
        }

        items = WhereFeatureMatches(items, "Brand", criteria.Brand);
        items = WhereFeatureMatches(items, "Model", criteria.Model);
        items = WhereFeatureMatches(items, "Color", criteria.Color);

        var results = await items
            .OrderByDescending(item => item.EventDate)
            .ThenByDescending(item => item.CreatedAt)
            .ToListAsync();

        return new ItemSearchResult { Items = results };
    }

    public Task<LostItem?> GetByIdAsync(int id) =>
        _dbContext.LostItems
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Features.Where(feature => !feature.IsHidden))
            .SingleOrDefaultAsync(item => item.Id == id);

    public async Task<IReadOnlyList<RecommendedItem>> GetRecommendationsAsync(LostItem item, int limit = 5)
    {
        if (limit <= 0)
        {
            return [];
        }

        var candidates = await _dbContext.LostItems
            .AsNoTracking()
            .Where(candidate =>
                candidate.Id != item.Id &&
                candidate.UserId != item.UserId &&
                candidate.IsFound != item.IsFound &&
                !candidate.IsDelivered &&
                (candidate.IsFound || !candidate.IsItemFound))
            .Include(candidate => candidate.Category)
            .Include(candidate => candidate.Features.Where(feature => !feature.IsHidden))
            .ToListAsync();

        return candidates
            .DistinctBy(candidate => candidate.Id)
            .Select(candidate => new RecommendedItem
            {
                Item = candidate,
                SimilarityPercent = _similarityService.CalculateScore(item, candidate)
            })
            .Where(candidate => candidate.SimilarityPercent >= MinimumRecommendationScore)
            .OrderByDescending(candidate => candidate.SimilarityPercent)
            .ThenByDescending(candidate => candidate.Item.EventDate)
            .Take(limit)
            .ToList();
    }

    public async Task<bool> CanChatAsync(string userId, int itemId, int matchedItemId)
    {
        if (string.IsNullOrWhiteSpace(userId) || itemId == matchedItemId)
        {
            return false;
        }

        var items = await _dbContext.LostItems
            .AsNoTracking()
            .Where(item => item.Id == itemId || item.Id == matchedItemId)
            .Include(item => item.Category)
            .Include(item => item.Features)
            .ToListAsync();

        if (items.Count != 2)
        {
            return false;
        }

        var item = items.Single(candidate => candidate.Id == itemId);
        var matchedItem = items.Single(candidate => candidate.Id == matchedItemId);
        if (!CanChatBetween(item, matchedItem, userId))
        {
            return false;
        }

        var foundItem = item.IsFound ? item : matchedItem;
        var lostItem = item.IsFound ? matchedItem : item;
        var requiresVerification = foundItem.Features.Any(feature => feature.Key == "SecurityQuestion");
        var verified = !requiresVerification || await _dbContext.VerifiedItemMatches.AnyAsync(match =>
            match.FoundItemId == foundItem.Id &&
            match.LostItemId == lostItem.Id &&
            match.UserId == lostItem.UserId);
        return verified && (userId == foundItem.UserId || userId == lostItem.UserId);
    }

    public async Task<string?> GetMatchVerificationQuestionAsync(string userId, int lostItemId, int foundItemId)
    {
        var pair = await _dbContext.LostItems
            .AsNoTracking()
            .Where(item => item.Id == lostItemId || item.Id == foundItemId)
            .Include(item => item.Features)
            .ToListAsync();

        if (pair.Count != 2)
        {
            return null;
        }

        var lostItem = pair.SingleOrDefault(item => !item.IsFound && item.Id == lostItemId);
        var foundItem = pair.SingleOrDefault(item => item.IsFound && item.Id == foundItemId);
        if (lostItem is null || foundItem is null ||
            (lostItem.UserId != userId && foundItem.UserId != userId) ||
            foundItem.UserId == lostItem.UserId || foundItem.IsDelivered)
        {
            return null;
        }

        if (_similarityService.CalculateScore(lostItem, foundItem) < MinimumChatMatchScore)
        {
            return null;
        }

        return foundItem.Features
            .Where(feature => feature.Key == "SecurityQuestion")
            .Select(feature => feature.Value)
            .FirstOrDefault();
    }

    public Task<bool> IsMatchVerifiedAsync(int lostItemId, int foundItemId, string userId) =>
        _dbContext.VerifiedItemMatches.AnyAsync(match =>
            match.FoundItemId == foundItemId &&
            match.LostItemId == lostItemId &&
            match.UserId == userId);

    public async Task<VerificationResult> VerifyLostItemMatchAsync(string userId, int lostItemId, int foundItemId, string answer)
    {
        var lostItem = await _dbContext.LostItems
            .SingleOrDefaultAsync(item => item.Id == lostItemId && !item.IsFound);
        var foundItem = await _dbContext.LostItems
            .Include(item => item.Features)
            .SingleOrDefaultAsync(item => item.Id == foundItemId && item.IsFound);

        if (lostItem is null || foundItem is null ||
            lostItem.UserId != userId || foundItem.UserId == lostItem.UserId ||
            foundItem.IsDelivered)
        {
            return new VerificationResult(false, "Bu eşleşme için doğrulama yetkiniz bulunmuyor.");
        }

        var alreadyVerified = await _dbContext.VerifiedItemMatches.AnyAsync(match =>
            match.FoundItemId == foundItemId &&
            match.LostItemId == lostItemId &&
            match.UserId == userId);

        if (!alreadyVerified)
        {
            _dbContext.VerifiedItemMatches.Add(new VerifiedItemMatch
            {
                FoundItemId = foundItemId,
                LostItemId = lostItemId,
                UserId = userId,
                VerifiedAt = DateTime.UtcNow
            });

            var answerHash = foundItem.Features
                .SingleOrDefault(feature => feature.Key == "SecurityAnswerHash")?.Value;
            var isCorrect = !string.IsNullOrWhiteSpace(answerHash) &&
                new PasswordHasher<LostItem>().VerifyHashedPassword(foundItem, answerHash, answer.Trim()) != PasswordVerificationResult.Failed;

            _dbContext.ContactMessages.Add(new ContactMessage
            {
                LostItemId = lostItemId,
                MatchedLostItemId = foundItemId,
                SenderUserId = userId,
                FromUserId = "Sistem",
                Message = isCorrect ? "Soru doğru cevaplandı." : "Soru yanlış cevaplandı.",
                SentAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();
        }

        // Cevap, iletişim izni için kaydedilir; doğru/yanlış olması sohbeti engellemez.
        return new VerificationResult(true, CanStartChat: true);
    }

    public async Task<IReadOnlyList<ChatMatch>> GetChatMatchesAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return [];
        }

        var listings = await _dbContext.LostItems
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Features.Where(feature => !feature.IsHidden))
            .ToListAsync();

        var ownedListings = listings.Where(item => item.UserId == userId).ToArray();
        return ownedListings
            .SelectMany(item => listings
                .Where(candidate => candidate.UserId != userId)
                .Select(candidate => new ChatMatch
                {
                    Item = item,
                    MatchedItem = candidate,
                    SimilarityPercent = _similarityService.CalculateScore(item, candidate)
                }))
            .Where(match => CanChatBetween(match.Item, match.MatchedItem, userId))
            .OrderByDescending(match => match.SimilarityPercent)
            .ThenByDescending(match => match.Item.EventDate)
            .ToList();
    }

    public async Task<VerificationResult> VerifyFoundItemAsync(string userId, int foundItemId, string answer)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new VerificationResult(false, "Doğrulama yapmak için giriş yapmalısınız.");
        }

        var item = await _dbContext.LostItems
            .Include(candidate => candidate.Features)
            .SingleOrDefaultAsync(candidate => candidate.Id == foundItemId && candidate.IsFound);
        if (item is null || !IsOpen(item))
        {
            return new VerificationResult(false, "Bu bulunan ilan artık doğrulanamıyor.");
        }

        if (item.UserId == userId)
        {
            return new VerificationResult(false, "Kendi ilanınızı doğrulayamazsınız.");
        }

        var answerHash = item.Features
            .SingleOrDefault(feature => feature.Key == "SecurityAnswerHash")?.Value;
        if (string.IsNullOrWhiteSpace(answerHash) || string.IsNullOrWhiteSpace(answer))
        {
            return new VerificationResult(false, "Doğrulama cevabı geçersiz.");
        }

        var submittedAnswer = answer.Trim();
        var passwordHasher = new PasswordHasher<LostItem>();
        var isCorrect = passwordHasher.VerifyHashedPassword(item, answerHash, submittedAnswer) != PasswordVerificationResult.Failed;
        var alreadyVerified = await _dbContext.VerifiedItemMatches
            .AnyAsync(match => match.FoundItemId == foundItemId && match.UserId == userId);
        if (!alreadyVerified)
        {
            _dbContext.VerifiedItemMatches.Add(new VerifiedItemMatch
            {
                FoundItemId = foundItemId,
                UserId = userId,
                VerifiedAt = DateTime.UtcNow
            });
        }

        var displayName = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(profile => profile.Id == userId)
            .Select(profile => profile.DisplayName)
            .SingleOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return new VerificationResult(false, "Sohbete katılmak için kullanıcı profili bulunamadı.");
        }

        _dbContext.ContactMessages.Add(new ContactMessage
        {
            LostItemId = foundItemId,
            SenderUserId = userId,
            FromUserId = displayName,
            Message = $"🔐 Doğrulama cevabı: {submittedAnswer} ({(isCorrect ? "doğru" : "yanlış")})",
            SentAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();

        return isCorrect
            ? new VerificationResult(true, CanStartChat: true)
            : new VerificationResult(false, "Cevabınız kaydedildi. İlan sahibiyle sohbet başlatabilirsiniz.", true);
    }

    public async Task<bool> CanChatWithVerifiedItemAsync(string userId, int foundItemId, string participantUserId)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(participantUserId))
        {
            return false;
        }

        var item = await _dbContext.LostItems
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == foundItemId && candidate.IsFound);
        if (item is null || !IsOpen(item))
        {
            return false;
        }

        var verified = await _dbContext.VerifiedItemMatches
            .AnyAsync(match => match.FoundItemId == foundItemId && match.UserId == participantUserId);
        return verified && (userId == participantUserId || userId == item.UserId);
    }

    public async Task<IReadOnlyList<VerifiedChatMatch>> GetVerifiedChatMatchesAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return [];
        }

        var matches = await _dbContext.VerifiedItemMatches
            .AsNoTracking()
            .Where(match => match.UserId == userId || match.FoundItem.UserId == userId)
            .Include(match => match.FoundItem)
                .ThenInclude(item => item.Category)
            .Include(match => match.FoundItem)
                .ThenInclude(item => item.Features.Where(feature => !feature.IsHidden))
            .Where(match => !match.FoundItem.IsDelivered)
            .OrderByDescending(match => match.VerifiedAt)
            .ToListAsync();

        var participantIds = matches.Select(match => match.UserId).Distinct().ToArray();
        var names = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(profile => participantIds.Contains(profile.Id))
            .ToDictionaryAsync(profile => profile.Id, profile => profile.DisplayName);

        return matches
            .Select(match => new VerifiedChatMatch
            {
                FoundItem = match.FoundItem,
                ParticipantUserId = match.UserId,
                ParticipantDisplayName = names.GetValueOrDefault(match.UserId, "Doğrulanan kullanıcı")
            })
            .ToList();
    }

    private bool CanChatBetween(LostItem item, LostItem matchedItem, string userId)
    {
        if (item.IsFound == matchedItem.IsFound ||
            item.UserId == matchedItem.UserId ||
            (item.UserId != userId && matchedItem.UserId != userId) ||
            !IsOpen(item) ||
            !IsOpen(matchedItem))
        {
            return false;
        }

        return _similarityService.CalculateScore(item, matchedItem) >= MinimumChatMatchScore;
    }

    private static bool IsOpen(LostItem item) =>
        !item.IsDelivered && (item.IsFound || !item.IsItemFound);

    private static IQueryable<LostItem> WhereFeatureMatches(
        IQueryable<LostItem> items,
        string key,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return items;
        }

        var normalizedKey = Normalize(key);
        var normalizedValue = Normalize(value);
        return items.Where(item => item.Features.Any(feature =>
            !feature.IsHidden &&
            feature.Key.ToLower() == normalizedKey &&
            feature.Value.ToLower().Contains(normalizedValue)));
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
