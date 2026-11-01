using System;
using System.Linq;
using System.Threading.Tasks;
using ARABULUCU.BLL.DTOs;
using ARABULUCU.BLL.Services;
using ARABULUCU.DAL.Data;
using ARABULUCU.DAL.Repositories;
using ARABULUCU.ENT.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ARABULUCU.Tests;

public sealed class LostItemListingServiceTests
{
    [Fact]
    public async Task SearchAsync_FiltersTitleDescriptionCategoryAndLocationCaseInsensitively()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var db = CreateContext(connection);
        var (owner, wallet, keys) = AddBaseEntities(db);
        db.LostItems.AddRange(
            Item(owner, wallet, "Blue wallet", "Contains a library card", "Central Library, second floor", new DateTime(2026, 10, 20)),
            Item(owner, keys, "Silver keys", "Three keys on a ring", "Cafeteria entrance", new DateTime(2026, 10, 21)));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var titleResults = await service.SearchAsync(new ItemSearchCriteria { Query = "BLUE" });
        var descriptionResults = await service.SearchAsync(new ItemSearchCriteria { Query = "LIBRARY CARD" });
        var categoryResults = await service.SearchAsync(new ItemSearchCriteria { Category = "wallets" });
        var locationResults = await service.SearchAsync(new ItemSearchCriteria { Location = "LIBRARY, SECOND" });

        Assert.Single(titleResults.Items);
        Assert.Single(descriptionResults.Items);
        Assert.Single(categoryResults.Items);
        Assert.Single(locationResults.Items);
    }

    [Fact]
    public async Task SearchAsync_CombinesFiltersAndSupportsVisibleFeaturesAndStatuses()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var db = CreateContext(connection);
        var (owner, wallet, keys) = AddBaseEntities(db);
        var matching = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", new DateTime(2026, 10, 20));
        matching.IsFound = false;
        matching.IsItemFound = false;
        matching.IsDelivered = false;
        matching.Features.Add(new LostItemFeature { Key = "Brand", Value = "Acme" });
        matching.Features.Add(new LostItemFeature { Key = "Color", Value = "Navy blue" });
        var other = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", new DateTime(2026, 10, 20));
        other.Features.Add(new LostItemFeature { Key = "Brand", Value = "Other" });
        var differentType = Item(owner, keys, "Blue wallet", "Leather", "Central Library", new DateTime(2026, 10, 20));
        differentType.IsFound = true;
        differentType.IsItemFound = true;
        differentType.IsDelivered = true;
        db.LostItems.AddRange(matching, other, differentType);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.SearchAsync(new ItemSearchCriteria
        {
            Query = "wallet",
            Category = "wallets",
            Location = "library",
            IsFound = false,
            Brand = "ACME",
            Color = "blue",
            IsItemFound = false,
            IsDelivered = false
        });

        var selected = Assert.Single(result.Items);
        Assert.Equal(matching.Id, selected.Id);
    }

    [Fact]
    public async Task SearchAsync_FoundItemsApplyPublicFiltersAndIgnoreHiddenMetadataFilters()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var db = CreateContext(connection);
        var (owner, wallet, _) = AddBaseEntities(db);
        var found = Item(
            owner,
            wallet,
            "Blue thermos",
            "Private detail word",
            "Private location",
            new DateTime(2026, 10, 20));
        found.IsFound = true;
        found.IsItemFound = true;
        found.IsDelivered = true;
        found.Features.Add(new LostItemFeature { Key = "Brand", Value = "Private brand", IsHidden = true });
        db.LostItems.Add(found);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var nonMatchingPublicFilters = await service.SearchAsync(new ItemSearchCriteria
        {
            Query = "thermos",
            Category = "a category that does not match",
            Location = "a location that does not match",
            EventDateFrom = new DateTime(2030, 1, 1),
            IsItemFound = false,
            IsDelivered = false,
            Brand = "a brand that does not match"
        });
        var titleSearch = await service.SearchAsync(new ItemSearchCriteria { Query = "thermos" });
        var descriptionSearch = await service.SearchAsync(new ItemSearchCriteria
        {
            Query = "Private detail word"
        });

        Assert.DoesNotContain(nonMatchingPublicFilters.Items, item => item.Id == found.Id);
        Assert.Contains(titleSearch.Items, item => item.Id == found.Id);
        Assert.Contains(descriptionSearch.Items, item => item.Id == found.Id);
        Assert.DoesNotContain(titleSearch.Items.SelectMany(item => item.Features), feature => feature.IsHidden);
    }

    [Fact]
    public async Task SearchAsync_FoundItemsMatchDescriptionAndCombineAllFilters()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var db = CreateContext(connection);
        var (owner, wallet, keys) = AddBaseEntities(db);
        var matching = Item(owner, wallet, "Blue thermos", "Contains a library card", "Central Library", new DateTime(2026, 10, 20));
        matching.IsFound = true;
        matching.IsItemFound = true;
        var wrongCategory = Item(owner, keys, "Blue thermos", "Contains a library card", "Central Library", new DateTime(2026, 10, 20));
        wrongCategory.IsFound = true;
        wrongCategory.IsItemFound = true;
        var wrongLocation = Item(owner, wallet, "Blue thermos", "Contains a library card", "Cafeteria", new DateTime(2026, 10, 20));
        wrongLocation.IsFound = true;
        wrongLocation.IsItemFound = true;
        db.LostItems.AddRange(matching, wrongCategory, wrongLocation);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var descriptionResults = await service.SearchAsync(new ItemSearchCriteria { Query = "LIBRARY CARD" });
        var combined = await service.SearchAsync(new ItemSearchCriteria
        {
            Query = "THERMOS",
            Category = "WALLETS",
            Location = "LIBRARY",
            IsFound = true,
            EventDateFrom = new DateTime(2026, 10, 20),
            EventDateTo = new DateTime(2026, 10, 20),
            IsItemFound = true,
            IsDelivered = false
        });

        Assert.Equal(3, descriptionResults.Items.Count);
        Assert.Equal(matching.Id, Assert.Single(combined.Items).Id);
    }

    [Fact]
    public async Task SearchAsync_HiddenFeatureCannotMatchAndIsNotReturned()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var db = CreateContext(connection);
        var (owner, wallet, _) = AddBaseEntities(db);
        var item = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        item.Features.Add(new LostItemFeature { Key = "Brand", Value = "Private brand", IsHidden = true });
        item.Features.Add(new LostItemFeature { Key = "Color", Value = "Navy", IsHidden = false });
        db.LostItems.Add(item);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var hiddenMatch = await service.SearchAsync(new ItemSearchCriteria { Brand = "private" });
        var visibleMatch = await service.SearchAsync(new ItemSearchCriteria { Color = "navy" });

        Assert.Empty(hiddenMatch.Items);
        Assert.DoesNotContain(Assert.Single(visibleMatch.Items).Features, feature => feature.IsHidden);
    }

    [Fact]
    public async Task SearchAsync_IncludesEventDateBoundsAndRejectsAnInvalidRange()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var db = CreateContext(connection);
        var (owner, wallet, _) = AddBaseEntities(db);
        db.LostItems.AddRange(
            Item(owner, wallet, "Earlier", "Item", "Library", new DateTime(2026, 10, 19, 23, 59, 0)),
            Item(owner, wallet, "Start", "Item", "Library", new DateTime(2026, 10, 20, 0, 0, 0)),
            Item(owner, wallet, "End", "Item", "Library", new DateTime(2026, 10, 21, 23, 59, 0)),
            Item(owner, wallet, "Later", "Item", "Library", new DateTime(2026, 10, 22, 0, 0, 0)));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var ranged = await service.SearchAsync(new ItemSearchCriteria
        {
            EventDateFrom = new DateTime(2026, 10, 20),
            EventDateTo = new DateTime(2026, 10, 21)
        });
        var invalid = await service.SearchAsync(new ItemSearchCriteria
        {
            EventDateFrom = new DateTime(2026, 10, 22),
            EventDateTo = new DateTime(2026, 10, 20)
        });

        Assert.Equal(2, ranged.Items.Count);
        Assert.Null(ranged.ValidationError);
        Assert.Empty(invalid.Items);
        Assert.Equal("Başlangıç tarihi bitiş tarihinden sonra olamaz.", invalid.ValidationError);
    }

    [Fact]
    public async Task SearchAsync_LeavesEmptyCriteriaUnfilteredAndKeepsFoundAndDeliveredIndependent()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var db = CreateContext(connection);
        var (owner, wallet, _) = AddBaseEntities(db);
        var foundButNotDelivered = Item(owner, wallet, "Found", "Wallet", "Library", DateTime.Today);
        foundButNotDelivered.IsFound = true;
        foundButNotDelivered.IsItemFound = true;
        foundButNotDelivered.IsDelivered = false;
        var lostNotFound = Item(owner, wallet, "Lost", "Wallet", "Library", DateTime.Today);
        lostNotFound.IsFound = false;
        lostNotFound.IsItemFound = false;
        lostNotFound.IsDelivered = false;
        db.LostItems.AddRange(foundButNotDelivered, lostNotFound);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var all = await service.SearchAsync(new ItemSearchCriteria());
        var found = await service.SearchAsync(new ItemSearchCriteria { IsItemFound = true, IsDelivered = false });

        Assert.Equal(2, all.Items.Count);
        Assert.Single(found.Items);
        Assert.True(found.Items[0].IsFound);
        Assert.True(found.Items[0].IsItemFound);
        Assert.False(found.Items[0].IsDelivered);
    }

    [Fact]
    public async Task GetRecommendationsAsync_OnlyReturnsUniqueOppositeTypeOpenItemsAndHandlesMissingItems()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var db = CreateContext(connection);
        var (owner, wallet, keys) = AddBaseEntities(db);
        var otherOwner = new UserProfile { Id = "other", DisplayName = "Other", Email = "other@example.com" };
        db.UserProfiles.Add(otherOwner);
        var lost = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        var found = Item(otherOwner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        found.IsFound = true;
        found.IsItemFound = true;
        var ownFound = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        ownFound.IsFound = true;
        ownFound.IsItemFound = true;
        var lostOtherType = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        lostOtherType.IsItemFound = true;
        var delivered = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        delivered.IsFound = true;
        delivered.IsItemFound = true;
        delivered.IsDelivered = true;
        var sameType = Item(owner, keys, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        db.LostItems.AddRange(lost, found, ownFound, lostOtherType, delivered, sameType);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var recommendations = await service.GetRecommendationsAsync(lost);

        Assert.Single(recommendations);
        Assert.Equal(found.Id, recommendations[0].Item.Id);
        Assert.True(recommendations[0].Item.IsFound);
        Assert.DoesNotContain(recommendations, recommendation => recommendation.Item.UserId == owner.Id);
        Assert.Null(await service.GetByIdAsync(int.MaxValue));

        found.IsDelivered = true;
        await db.SaveChangesAsync();
        Assert.Empty(await service.GetRecommendationsAsync(lost));
    }

    [Fact]
    public async Task GetRecommendationsAsync_MatchesOppositeTypesInBothDirections()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var db = CreateContext(connection);
        var (owner, wallet, _) = AddBaseEntities(db);
        var other = new UserProfile { Id = "other", DisplayName = "Other", Email = "other@example.com" };
        db.UserProfiles.Add(other);
        var lost = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        var found = Item(other, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        found.IsFound = true;
        found.IsItemFound = true;
        var otherLost = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        var otherFound = Item(other, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        otherFound.IsFound = true;
        otherFound.IsItemFound = true;
        db.LostItems.AddRange(lost, found, otherLost, otherFound);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var lostRecommendations = await service.GetRecommendationsAsync(lost);
        var foundRecommendations = await service.GetRecommendationsAsync(found);

        Assert.Equal(2, lostRecommendations.Count);
        Assert.All(lostRecommendations, recommendation => Assert.True(recommendation.Item.IsFound));
        Assert.Equal(2, foundRecommendations.Count);
        Assert.All(foundRecommendations, recommendation => Assert.False(recommendation.Item.IsFound));
    }

    [Theory]
    [InlineData(49.99, false)]
    [InlineData(50, true)]
    [InlineData(50.01, true)]
    public async Task GetRecommendationsAsync_OnlyReturnsRecommendationsAtOrAboveFiftyPercent(
        double score,
        bool expectedVisible)
    {
        await using var connection = await CreateDatabaseAsync();
        await using var db = CreateContext(connection);
        var (owner, wallet, _) = AddBaseEntities(db);
        var other = new UserProfile { Id = "other", DisplayName = "Other", Email = "other@example.com" };
        db.UserProfiles.Add(other);
        var source = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        var candidate = Item(other, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        candidate.IsFound = true;
        candidate.IsItemFound = true;
        db.LostItems.AddRange(source, candidate);
        await db.SaveChangesAsync();
        var service = new LostItemListingService(
            db,
            new ItemRepository(db),
            new FixedScoreSimilarityService((decimal)score));

        var recommendations = await service.GetRecommendationsAsync(source);

        Assert.Equal(expectedVisible, recommendations.Count == 1);
        if (expectedVisible)
        {
            Assert.Equal((decimal)score, Assert.Single(recommendations).SimilarityPercent);
        }
    }

    [Fact]
    public async Task CanChatAsync_OnlyAllowsOwnersOfOpenOppositeTypeMatches()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var db = CreateContext(connection);
        var (owner, wallet, _) = AddBaseEntities(db);
        var finder = new UserProfile { Id = "finder", DisplayName = "Finder", Email = "finder@example.com" };
        var stranger = new UserProfile { Id = "stranger", DisplayName = "Stranger", Email = "stranger@example.com" };
        db.UserProfiles.AddRange(finder, stranger);
        var lost = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        var found = Item(finder, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        found.IsFound = true;
        found.IsItemFound = true;
        var anotherLost = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        var anotherFound = Item(finder, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        anotherFound.IsFound = true;
        anotherFound.IsItemFound = true;
        db.LostItems.AddRange(lost, found, anotherLost, anotherFound);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        Assert.True(await service.CanChatAsync(owner.Id, lost.Id, found.Id));
        Assert.True(await service.CanChatAsync(finder.Id, found.Id, lost.Id));
        Assert.False(await service.CanChatAsync(stranger.Id, lost.Id, found.Id));
        Assert.False(await service.CanChatAsync(owner.Id, lost.Id, lost.Id));
        Assert.False(await service.CanChatAsync(owner.Id, lost.Id, anotherLost.Id));
        Assert.False(await service.CanChatAsync(finder.Id, found.Id, anotherFound.Id));

        found.IsDelivered = true;
        await db.SaveChangesAsync();
        Assert.False(await service.CanChatAsync(owner.Id, lost.Id, found.Id));

        found.IsDelivered = false;
        lost.IsItemFound = true;
        await db.SaveChangesAsync();
        Assert.False(await service.CanChatAsync(owner.Id, lost.Id, found.Id));
    }

    [Theory]
    [InlineData(49.99, false)]
    [InlineData(50, true)]
    [InlineData(50.01, true)]
    public async Task CanChatAsync_EnforcesFiftyPercentSimilarityBoundary(double score, bool expected)
    {
        await using var connection = await CreateDatabaseAsync();
        await using var db = CreateContext(connection);
        var (owner, wallet, _) = AddBaseEntities(db);
        var finder = new UserProfile { Id = "finder", DisplayName = "Finder", Email = "finder@example.com" };
        db.UserProfiles.Add(finder);
        var lost = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        var found = Item(finder, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        found.IsFound = true;
        found.IsItemFound = true;
        db.LostItems.AddRange(lost, found);
        await db.SaveChangesAsync();
        var service = new LostItemListingService(
            db,
            new ItemRepository(db),
            new FixedScoreSimilarityService((decimal)score));

        Assert.Equal(expected, await service.CanChatAsync(owner.Id, lost.Id, found.Id));
    }

    [Fact]
    public async Task GetChatMatchesAsync_OnlyReturnsOpenOppositeTypeItems()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var db = CreateContext(connection);
        var (owner, wallet, _) = AddBaseEntities(db);
        var other = new UserProfile { Id = "other", DisplayName = "Other", Email = "other@example.com" };
        db.UserProfiles.Add(other);
        var ownLost = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        var ownFound = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        ownFound.IsFound = true;
        ownFound.IsItemFound = true;
        var otherLost = Item(other, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        var otherFound = Item(other, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        otherFound.IsFound = true;
        otherFound.IsItemFound = true;
        var closedLost = Item(other, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        closedLost.IsItemFound = true;
        var closedFound = Item(other, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        closedFound.IsFound = true;
        closedFound.IsItemFound = true;
        closedFound.IsDelivered = true;
        db.LostItems.AddRange(ownLost, ownFound, otherLost, otherFound, closedLost, closedFound);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var matches = await service.GetChatMatchesAsync(owner.Id);

        Assert.Equal(2, matches.Count);
        Assert.All(matches, match => Assert.NotEqual(match.Item.IsFound, match.MatchedItem.IsFound));
        Assert.All(matches, match => Assert.True(match.Item.IsFound || !match.Item.IsItemFound));
        Assert.All(matches, match => Assert.True(match.MatchedItem.IsFound || !match.MatchedItem.IsItemFound));
        Assert.All(matches, match => Assert.False(match.Item.IsDelivered || match.MatchedItem.IsDelivered));
    }

    [Fact]
    public async Task GetByIdAsyncAndSimilarity_ExcludeHiddenFeatures()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var db = CreateContext(connection);
        var (owner, wallet, _) = AddBaseEntities(db);
        var item = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        item.Features.Add(new LostItemFeature { Key = "Color", Value = "Navy", IsHidden = false });
        item.Features.Add(new LostItemFeature { Key = "Secret", Value = "hidden phrase", IsHidden = true });
        db.LostItems.Add(item);
        await db.SaveChangesAsync();
        var service = CreateService(db);
        var loaded = await service.GetByIdAsync(item.Id);
        var other = Item(owner, wallet, "Blue wallet", "Leather", "Central Library", DateTime.Today);
        var scorer = new LostItemSimilarityService();

        Assert.NotNull(loaded);
        Assert.Contains(loaded.Features, feature => feature.Key == "Color");
        Assert.DoesNotContain(loaded.Features, feature => feature.IsHidden);
        Assert.Equal(
            scorer.CalculateScore(item, other),
            scorer.CalculateScore(loaded, other));
    }

    private static LostItemListingService CreateService(ApplicationDbContext db) =>
        new(db, new ItemRepository(db), new LostItemSimilarityService());

    private sealed class FixedScoreSimilarityService(decimal score) : ILostItemSimilarityService
    {
        public decimal CalculateScore(LostItem first, LostItem second) => score;
    }

    private static ApplicationDbContext CreateContext(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);

    private static async Task<SqliteConnection> CreateDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync();
        await db.DisposeAsync();
        return connection;
    }

    private static (UserProfile Owner, Category Wallet, Category Keys) AddBaseEntities(ApplicationDbContext db)
    {
        var owner = new UserProfile { Id = "owner", DisplayName = "Owner", Email = "owner@example.com" };
        var wallet = new Category { Id = 101, Name = "Wallets" };
        var keys = new Category { Id = 102, Name = "Keys" };
        db.UserProfiles.Add(owner);
        db.Categories.AddRange(wallet, keys);
        return (owner, wallet, keys);
    }

    private static LostItem Item(
        UserProfile owner,
        Category category,
        string title,
        string description,
        string location,
        DateTime eventDate) =>
        new()
        {
            Title = title,
            Description = description,
            Location = location,
            EventDate = eventDate,
            CreatedAt = DateTime.UtcNow,
            User = owner,
            Category = category
        };
}
