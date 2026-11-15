using ARABULUCU.BLL.DTOs;
using ARABULUCU.DAL.Data;
using ARABULUCU.DAL.Repositories;
using ARABULUCU.ENT.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text;

namespace ARABULUCU.BLL.Services;

public sealed class LostItemListingService : ILostItemListingService
{
    private const decimal MinimumRecommendationScore = 50;
    private const decimal MinimumChatMatchScore = 50;
    private const int MaximumRecommendationCount = 5;
    private const int MinimumVerificationAnswers = 3;
    private const int MinimumVerificationScore = 50;
    private const int MaximumFailedVerificationAttempts = 5;
    private static readonly TimeSpan VerificationLockDuration = TimeSpan.FromMinutes(15);

    private readonly ApplicationDbContext _dbContext;
    private readonly IItemRepository _itemRepository;
    private readonly ILostItemSimilarityService _similarityService;
    private readonly PasswordHasher<LostItemFeature> _passwordHasher = new();

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
            .Where(item => !item.IsArchived)
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
            var from = criteria.EventDateFrom.Value;
            items = items.Where(item => item.EventDate.Date >= from);
        }

        if (criteria.EventDateTo.HasValue)
        {
            var to = criteria.EventDateTo.Value.TimeOfDay == TimeSpan.Zero
                ? criteria.EventDateTo.Value.Date.AddDays(1).AddTicks(-1)
                : criteria.EventDateTo.Value;
            items = items.Where(item => item.EventDate <= to);
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

        RemoveHiddenFeatures(results);

        return new ItemSearchResult { Items = results };
    }

    public async Task<LostItem?> GetByIdAsync(int id)
    {
        var item = await _dbContext.LostItems
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Features.Where(feature => !feature.IsHidden))
            .SingleOrDefaultAsync(item => item.Id == id);

        if (item is not null)
        {
            RemoveHiddenFeatures(item);
        }

        return item;
    }

    public async Task<IReadOnlyList<RecommendedItem>> GetRecommendationsAsync(LostItem item, int limit = 5)
    {
        if (limit <= 0 || !IsOpen(item))
        {
            return [];
        }

        var candidates = await _dbContext.LostItems
            .AsNoTracking()
            .Where(candidate =>
                candidate.Id != item.Id &&
                candidate.UserId != item.UserId &&
                !candidate.IsArchived &&
                candidate.IsFound != item.IsFound &&
                !candidate.IsDelivered &&
                (candidate.IsFound || !candidate.IsItemFound))
            .Include(candidate => candidate.Category)
            .Include(candidate => candidate.Features.Where(feature => !feature.IsHidden))
            .ToListAsync();

        RemoveHiddenFeatures(candidates);

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
            .Take(Math.Min(limit, MaximumRecommendationCount))
            .ToList();
    }

    public async Task<IReadOnlyList<VerificationQuestionDto>> GetVerificationQuestionsAsync(int itemId)
    {
        return await _dbContext.LostItemFeatures
            .AsNoTracking()
            // Eski ilanlarda bu bayrak bulunmadığından, yeni şema dışındaki
            // doğrulama alanlarını da okuyabilmeliyiz.
            .Where(feature => feature.LostItemId == itemId &&
                (feature.IsVerificationQuestion ||
                 feature.Key == "Brand" || feature.Key == "Model" || feature.Key == "Color" ||
                 feature.Key == "SecurityQuestion" || feature.Key == "SecurityAnswer"))
            .OrderBy(feature => feature.Id)
            .Select(feature => new VerificationQuestionDto
            {
                Id = feature.Id,
                Prompt = feature.Key == "SecurityQuestion"
                    ? feature.Value
                    : GetVerificationPrompt(feature.Key)
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<VerificationQuestionDto>> GetMatchingQuestionsAsync()
    {
        var features = await _dbContext.LostItemFeatures
            .AsNoTracking()
            .Where(feature => feature.IsVerificationQuestion ||
                feature.Key == "Brand" || feature.Key == "Model" || feature.Key == "Color" ||
                feature.Key == "SecurityQuestion" || feature.Key == "SecurityAnswer")
            .OrderBy(feature => feature.Id)
            .ToListAsync();

        return features
            .Select(feature => new VerificationQuestionDto
            {
                Id = feature.Id,
                Prompt = feature.Key == "SecurityQuestion"
                    ? feature.Value
                    : GetVerificationPrompt(feature.Key)
            })
            .GroupBy(question => question.Prompt, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    public async Task<MatchingResult> FindMatchesAsync(MatchingCriteria criteria)
    {
        if (criteria.Answers.Count > 0)
        {
            return await FindLegacyMatchesAsync(criteria);
        }

        if (string.IsNullOrWhiteSpace(criteria.Category) ||
            string.IsNullOrWhiteSpace(criteria.Title) ||
            string.IsNullOrWhiteSpace(criteria.Location))
        {
            return MatchingFailure("Kategori, eşya başlığı ve kampüs bölgesi zorunludur.");
        }

        var normalizedCategory = Normalize(criteria.Category);
        var normalizedTitle = Normalize(criteria.Title);
        var normalizedLocation = Normalize(criteria.Location);

        IQueryable<LostItem> candidatesQuery = _dbContext.LostItems
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Features)
            .Where(item => item.Category.Name.ToLower() == normalizedCategory &&
                item.Title.ToLower().Contains(normalizedTitle) &&
                item.Location.ToLower().Contains(normalizedLocation) &&
                item.EventDate.Date == criteria.EventDate.Date &&
                !item.IsDelivered &&
                !item.IsArchived);

        candidatesQuery = WhereFeatureMatches(candidatesQuery, "Brand", criteria.Brand);
        candidatesQuery = WhereFeatureMatches(candidatesQuery, "Model", criteria.Model);
        candidatesQuery = WhereFeatureMatches(candidatesQuery, "Color", criteria.Color);

        var candidates = await candidatesQuery.ToListAsync();
        var searchItem = new LostItem
        {
            Title = criteria.Title.Trim(),
            Description = criteria.Title.Trim(),
            Location = criteria.Location.Trim(),
            EventDate = criteria.EventDate,
            Category = new Category { Name = criteria.Category.Trim() }
        };

        var matches = candidates
            .Select(item => new
            {
                Item = item,
                Score = _similarityService.CalculateScore(searchItem, item)
            })
            .Where(candidate => candidate.Score >= MinimumRecommendationScore)
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Item.EventDate)
            .Take(MaximumRecommendationCount)
            .Select(candidate => candidate.Item)
            .ToList();

        if (matches.Count == 0)
        {
            return MatchingFailure("Seçtiğiniz kriterlerle eşleşen ilan bulunamadı.");
        }

        RemoveHiddenFeatures(matches);
        return new MatchingResult { IsSuccessful = true, Items = matches };
    }

    private async Task<MatchingResult> FindLegacyMatchesAsync(MatchingCriteria criteria)
    {
        if (string.IsNullOrWhiteSpace(criteria.Location))
        {
            return MatchingFailure("Konum zorunludur.");
        }

        var normalizedLocation = Normalize(criteria.Location);
        var answerByQuestion = criteria.Answers
            .Where(answer => !string.IsNullOrWhiteSpace(answer.Answer))
            .GroupBy(answer => answer.QuestionId)
            .ToDictionary(group => group.Key, group => group.Last().Answer.Trim());

        var candidates = await _dbContext.LostItems
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Features)
            .Where(item => item.Location.ToLower().Contains(normalizedLocation) &&
                item.EventDate.Date == criteria.EventDate.Date &&
                !item.IsDelivered &&
                !item.IsArchived)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        var matches = candidates
            .Select(item => new
            {
                Item = item,
                Questions = item.Features.Where(feature =>
                    feature.IsVerificationQuestion ||
                    feature.Key == "Brand" || feature.Key == "Model" || feature.Key == "Color" ||
                    feature.Key == "SecurityQuestion" || feature.Key == "SecurityAnswer").ToList()
            })
            .Select(candidate => new
            {
                candidate.Item,
                CorrectCount = candidate.Questions.Count(question =>
                    answerByQuestion.TryGetValue(question.Id, out var answer) && IsAnswerCorrect(question, answer)),
                Score = candidate.Questions
                    .Where(question => answerByQuestion.TryGetValue(question.Id, out var answer) && IsAnswerCorrect(question, answer))
                    .Sum(GetVerificationWeight)
            })
            .Where(candidate => candidate.CorrectCount >= MinimumVerificationAnswers &&
                candidate.Score >= MinimumVerificationScore)
            .Select(candidate => candidate.Item)
            .ToList();

        if (matches.Count == 0)
        {
            return MatchingFailure("Konum, tarih ve doğrulama cevaplarıyla eşleşen ilan bulunamadı.");
        }

        RemoveHiddenFeatures(matches);
        return new MatchingResult { IsSuccessful = true, Items = matches };
    }

    public async Task<VerificationResult> VerifyAsync(
        int itemId,
        string claimerUserId,
        IReadOnlyCollection<VerificationAnswerDto> answers)
    {
        if (string.IsNullOrWhiteSpace(claimerUserId))
        {
            return Failure("Doğrulama için giriş yapmalısınız.");
        }

        var item = await _dbContext.LostItems
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == itemId);
        if (item is null || item.UserId == claimerUserId)
        {
            return Failure("Bu ilan için doğrulama yapılamıyor.");
        }

        var attempt = await _dbContext.VerificationAttempts
            .SingleOrDefaultAsync(value => value.LostItemId == itemId && value.ClaimerUserId == claimerUserId);
        if (attempt?.LockedUntil > DateTime.UtcNow)
        {
            var remainingMinutes = Math.Max(1, (int)Math.Ceiling((attempt.LockedUntil!.Value - DateTime.UtcNow).TotalMinutes));
            return Failure($"Çok fazla hatalı deneme yapıldı. Lütfen {remainingMinutes} dakika sonra tekrar deneyin.");
        }

        var questions = await _dbContext.LostItemFeatures
            .Where(feature => feature.LostItemId == itemId &&
                (feature.IsVerificationQuestion ||
                 feature.Key == "Brand" || feature.Key == "Model" || feature.Key == "Color" ||
                 feature.Key == "SecurityQuestion" || feature.Key == "SecurityAnswer"))
            .ToListAsync();

        var answerByQuestion = answers
            .Where(answer => !string.IsNullOrWhiteSpace(answer.Answer))
            .GroupBy(answer => answer.QuestionId)
            .ToDictionary(group => group.Key, group => group.Last().Answer.Trim());

        var correctCount = 0;
        var score = 0;
        foreach (var question in questions)
        {
            if (!answerByQuestion.TryGetValue(question.Id, out var answer) ||
                !IsAnswerCorrect(question, answer))
            {
                continue;
            }

            correctCount++;
            score += GetVerificationWeight(question);
        }

        if (correctCount < MinimumVerificationAnswers || score < MinimumVerificationScore)
        {
            attempt ??= new VerificationAttempt { LostItemId = itemId, ClaimerUserId = claimerUserId };
            attempt.FailedAttempts++;
            attempt.LastAttemptAt = DateTime.UtcNow;
            if (attempt.FailedAttempts >= MaximumFailedVerificationAttempts)
            {
                attempt.LockedUntil = DateTime.UtcNow.Add(VerificationLockDuration);
                attempt.FailedAttempts = 0;
            }
            if (attempt.Id == 0)
            {
                _dbContext.VerificationAttempts.Add(attempt);
            }
            await _dbContext.SaveChangesAsync();
            return Failure("Girdiğiniz bilgiler ilanla eşleşmedi.");
        }

        if (attempt is not null)
        {
            attempt.FailedAttempts = 0;
            attempt.LockedUntil = null;
            attempt.LastAttemptAt = DateTime.UtcNow;
        }

        var existingMatch = await _dbContext.VerificationMatches
            .SingleOrDefaultAsync(match => match.LostItemId == itemId && match.ClaimerUserId == claimerUserId);
        if (existingMatch is null)
        {
            _dbContext.VerificationMatches.Add(new VerificationMatch
            {
                LostItemId = itemId,
                ClaimerUserId = claimerUserId,
                CorrectAnswerCount = correctCount,
                Score = score
            });
        }
        else
        {
            existingMatch.CorrectAnswerCount = correctCount;
            existingMatch.Score = score;
            existingMatch.VerifiedAt = DateTime.UtcNow;
        }

        item.LastActivityAt = DateTime.UtcNow;
        _dbContext.ListingStatusHistories.Add(new ListingStatusHistory
        {
            LostItemId = itemId,
            Status = "Verified",
            ChangedByUserId = claimerUserId,
            Details = $"Doğrulama tamamlandı. Doğru cevap: {correctCount}, puan: {score}."
        });

        await _dbContext.SaveChangesAsync();
        return new VerificationResult { IsSuccessful = true };
    }

    private static int GetVerificationWeight(LostItemFeature feature) =>
        feature.Weight > 0
            ? feature.Weight
            : feature.Key is "SecurityQuestion" or "SecurityAnswer" ? 30 : 20;

    public async Task<ProtectedListingDto?> GetProtectedDetailsAsync(int itemId, string claimerUserId)
    {
        var verified = await _dbContext.VerificationMatches
            .AsNoTracking()
            .AnyAsync(match => match.LostItemId == itemId && match.ClaimerUserId == claimerUserId);
        if (!verified)
        {
            return null;
        }

        var item = await _dbContext.LostItems
            .AsNoTracking()
            .Include(candidate => candidate.Features)
            .SingleOrDefaultAsync(candidate => candidate.Id == itemId);
        if (item is null)
        {
            return null;
        }

        return new ProtectedListingDto
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            Location = item.Location,
            Features = item.Features
                .Where(feature => feature.IsHidden &&
                    feature.Key is not "Location" and not "EventDate" &&
                    !feature.Key.Contains("soru", StringComparison.OrdinalIgnoreCase) &&
                    feature.Key != "SecurityQuestion")
                .Select(feature => new ProtectedFeatureDto { Key = feature.Key, Value = feature.Value })
                .ToList()
        };
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

        RemoveHiddenFeatures(items);

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

        var itemToVerify = item.UserId == userId ? matchedItem.Id : item.Id;
        return await _dbContext.VerificationMatches
            .AsNoTracking()
            .AnyAsync(match => match.LostItemId == itemToVerify && match.ClaimerUserId == userId);
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

        RemoveHiddenFeatures(listings);

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
        !item.IsArchived && !item.IsDelivered && (item.IsFound || !item.IsItemFound);

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

    private static string GetVerificationPrompt(string key) => key switch
    {
        "Brand" => "Marka nedir?",
        "Model" => "Model nedir?",
        "Color" => "Rengi nedir?",
        "Location" => "İlanın konumu nedir?",
        "EventDate" => "Olayın tarihi ve saati nedir? (GG.AA.YYYY SS:DD)",
        "SecurityQuestion" or "SecurityAnswer" => "Gizli doğrulama detayı",
        _ => key
    };

    private bool IsAnswerCorrect(LostItemFeature feature, string answer)
    {
        var normalizedAnswer = NormalizeAnswer(answer);
        if (feature.AnswerHash is not null &&
            _passwordHasher.VerifyHashedPassword(feature, feature.AnswerHash, normalizedAnswer) != PasswordVerificationResult.Failed)
        {
            return true;
        }

        return feature.Key is not "SecurityQuestion" and not "SecurityAnswer" &&
            NormalizeAnswer(feature.Value) == normalizedAnswer;
    }

    private static string NormalizeAnswer(string value) =>
        string.Join(' ', value.Trim().Normalize(NormalizationForm.FormKC)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();

    private static VerificationResult Failure(string error) => new() { Error = error };

    private static MatchingResult MatchingFailure(string error) => new() { Error = error };

    // Filtered Include is a query concern; sanitize the public entity graph as
    // a final boundary so private metadata cannot leak through fixup or future
    // repository changes.
    private static void RemoveHiddenFeatures(IEnumerable<LostItem> items)
    {
        foreach (var item in items)
        {
            RemoveHiddenFeatures(item);
        }
    }

    private static void RemoveHiddenFeatures(LostItem item)
    {
        item.Features = item.Features
            .Where(feature => !feature.IsHidden)
            .ToList();
    }
}
