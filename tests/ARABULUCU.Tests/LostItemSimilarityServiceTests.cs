using System;
using ARABULUCU.BLL.Services;
using ARABULUCU.ENT.Entities;
using Xunit;

namespace ARABULUCU.Tests;

public sealed class LostItemSimilarityServiceTests
{
    [Fact]
    public void CalculateScore_AppliesBaseWeightsAndDateBonus()
    {
        var service = new LostItemSimilarityService();
        var first = Item("blue wallet", "leather item", "north hall", "Wallets");
        var second = Item("blue wallet", "leather item", "North Hall", "wallets");

        Assert.Equal(110m, service.CalculateScore(first, second));

        second.Category!.Name = "Keys";
        Assert.Equal(75m, service.CalculateScore(first, second));

        second.Category.Name = "Wallets";
        second.Location = "south hall";
        Assert.Equal(95m, service.CalculateScore(first, second));
    }

    [Fact]
    public void CalculateScore_RequiresTitleOrDescriptionSignal()
    {
        var service = new LostItemSimilarityService();
        var first = Item("blue wallet", "leather", "north hall", "Wallets");
        var second = Item("red keys", "metal", "North Hall", "wallets");

        Assert.Equal(0m, service.CalculateScore(first, second));
    }

    [Fact]
    public void CalculateScore_UsesTitleAndDescriptionAsIndependentSignals()
    {
        var service = new LostItemSimilarityService();
        var first = Item("blue wallet", "leather material", "", "");
        var second = Item("blue wallet", "metal", "", "");
        second.EventDate = first.EventDate.AddDays(7);

        Assert.Equal(25m, service.CalculateScore(first, second));

        second.Title = "red keys";
        second.Description = "leather material";
        Assert.Equal(25m, service.CalculateScore(first, second));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 8.57)]
    [InlineData(2, 7.14)]
    [InlineData(3, 5.71)]
    [InlineData(4, 4.29)]
    [InlineData(5, 2.86)]
    [InlineData(7, 0)]
    [InlineData(30, 0)]
    public void CalculateScore_AddsLinearlyDecreasingDateBonus(int dayDifference, double expectedBonus)
    {
        var service = new LostItemSimilarityService();
        var first = Item("blue wallet", "", "", "");
        var second = Item("blue wallet", "", "", "");
        second.EventDate = first.EventDate.AddDays(dayDifference);

        Assert.Equal(25m + (decimal)expectedBonus, service.CalculateScore(first, second));
    }

    [Fact]
    public void CalculateScore_UsesCaseInsensitiveSubstringLocationMatch()
    {
        var service = new LostItemSimilarityService();
        var first = Item("blue wallet", "", "Central Library, second floor", "");
        var second = Item("blue wallet", "", "central library", "");

        Assert.Equal(50m, service.CalculateScore(first, second));
    }

    private static LostItem Item(string title, string description, string location, string category) =>
        new()
        {
            Title = title,
            Description = description,
            Location = location,
            EventDate = new DateTime(2026, 11, 15),
            Category = new Category { Name = category }
        };
}
