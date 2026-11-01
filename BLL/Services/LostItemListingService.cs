using ARABULUCU.BLL.DTOs;
using ARABULUCU.DAL.Data;
using ARABULUCU.DAL.Repositories;
using ARABULUCU.ENT.Entities;
using Microsoft.EntityFrameworkCore;

namespace ARABULUCU.BLL.Services;

public sealed class LostItemListingService : ILostItemListingService
{
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
            .Include(item => item.Features.Where(feature => !feature.IsHidden))
            .ToListAsync();

        if (items.Count != 2)
        {
            return false;
        }

        var item = items.Single(candidate => candidate.Id == itemId);
        var matchedItem = items.Single(candidate => candidate.Id == matchedItemId);
        return CanChatBetween(item, matchedItem, userId);
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
