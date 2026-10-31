using ARABULUCU.BLL.DTOs;
using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;
using Microsoft.EntityFrameworkCore;

namespace ARABULUCU.BLL.Services;

public sealed class LostItemListingService : ILostItemListingService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILostItemSimilarityService _similarityService;

    public LostItemListingService(
        ApplicationDbContext dbContext,
        ILostItemSimilarityService similarityService)
    {
        _dbContext = dbContext;
        _similarityService = similarityService;
    }

    public async Task<ItemSearchResult> SearchAsync(ItemSearchCriteria criteria)
    {
        if (criteria.EventDateFrom.HasValue && criteria.EventDateTo.HasValue &&
            criteria.EventDateFrom.Value.Date > criteria.EventDateTo.Value.Date)
        {
            return new ItemSearchResult { ValidationError = "Başlangıç tarihi bitiş tarihinden sonra olamaz." };
        }

        var items = await _dbContext.LostItems
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Features.Where(feature => !feature.IsHidden))
            .ToListAsync();

        var results = items
            .Where(item => Matches(item, criteria))
            .OrderByDescending(item => item.EventDate)
            .ThenByDescending(item => item.CreatedAt)
            .ToList();

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

    private static bool Matches(LostItem item, ItemSearchCriteria criteria)
    {
        if (!string.IsNullOrWhiteSpace(criteria.Query) &&
            !Contains(item.Title, criteria.Query) &&
            (item.IsFound || !Contains(item.Description, criteria.Query)))
        {
            return false;
        }

        if (criteria.IsFound.HasValue && item.IsFound != criteria.IsFound.Value)
        {
            return false;
        }

        if (item.IsFound)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(criteria.Category) &&
            !string.Equals(item.Category?.Name, criteria.Category.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(criteria.Location) && !Contains(item.Location, criteria.Location))
        {
            return false;
        }

        if (criteria.IsItemFound.HasValue && item.IsItemFound != criteria.IsItemFound.Value ||
            criteria.IsDelivered.HasValue && item.IsDelivered != criteria.IsDelivered.Value)
        {
            return false;
        }

        if (criteria.EventDateFrom.HasValue && item.EventDate.Date < criteria.EventDateFrom.Value.Date ||
            criteria.EventDateTo.HasValue && item.EventDate.Date > criteria.EventDateTo.Value.Date)
        {
            return false;
        }

        return MatchesFeature(item, "Brand", criteria.Brand) &&
            MatchesFeature(item, "Model", criteria.Model) &&
            MatchesFeature(item, "Color", criteria.Color);
    }

    private static bool MatchesFeature(LostItem item, string key, string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        item.Features.Any(feature =>
            !feature.IsHidden &&
            string.Equals(feature.Key, key, StringComparison.OrdinalIgnoreCase) &&
            Contains(feature.Value, value));

    private static bool Contains(string? value, string search) =>
        value?.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase) == true;
}
