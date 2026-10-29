using System;
using ARABULUCU.BLL.Services;
using ARABULUCU.ENT.Entities;
using Xunit;

namespace ARABULUCU.Tests;

public sealed class LostItemSimilarityServiceTests
{
    [Fact]
    public void CalculateScore_GivesHigherScoreToEventsThatAreCloserInTime()
    {
        var service = new LostItemSimilarityService();
        var lost = Item("wallet", new DateTime(2026, 10, 20));
        var close = Item("wallet", new DateTime(2026, 10, 21));
        var distant = Item("wallet", new DateTime(2026, 11, 9));

        Assert.True(service.CalculateScore(lost, close) > service.CalculateScore(lost, distant));
        Assert.InRange(service.CalculateScore(lost, distant), 0, 100);
    }

    [Fact]
    public void CalculateScore_UsesEventDateAndNeverCreatedAt()
    {
        var service = new LostItemSimilarityService();
        var first = Item("wallet", new DateTime(2026, 10, 20));
        var second = Item("wallet", new DateTime(2026, 10, 21));
        var baseline = service.CalculateScore(first, second);

        second.CreatedAt = second.CreatedAt.AddYears(10);

        Assert.Equal(baseline, service.CalculateScore(first, second));
    }

    [Fact]
    public void CalculateScore_IgnoresHiddenFeaturesAndBoundsMissingData()
    {
        var service = new LostItemSimilarityService();
        var first = new LostItem
        {
            Title = "blue wallet",
            EventDate = new DateTime(2026, 10, 20)
        };
        var second = new LostItem
        {
            Title = "red keys",
            EventDate = new DateTime(2026, 10, 20)
        };
        var baseline = service.CalculateScore(first, second);
        first.Features.Add(new LostItemFeature { Key = "Secret", Value = "exact secret", IsHidden = true });
        second.Features.Add(new LostItemFeature { Key = "Secret", Value = "exact secret", IsHidden = true });

        Assert.Equal(baseline, service.CalculateScore(first, second));
        Assert.InRange(service.CalculateScore(first, second), 0, 100);
    }

    private static LostItem Item(string title, DateTime eventDate) =>
        new()
        {
            Title = title,
            Description = "Leather wallet",
            Location = "Central Library",
            Category = new Category { Name = "Wallets" },
            EventDate = eventDate,
            CreatedAt = DateTime.UtcNow
        };
}
