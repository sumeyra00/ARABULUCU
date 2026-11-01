using System;
using ARABULUCU.BLL.Services;
using ARABULUCU.ENT.Entities;
using Xunit;

namespace ARABULUCU.Tests;

public sealed class LostItemSimilarityServiceTests
{
    [Fact]
    public void CalculateScore_CategoryContributesItsNormalizedWeight()
    {
        var service = new LostItemSimilarityService();
        var first = Item("blue wallet", "leather", "north hall", "Wallets");
        var second = Item("red keys", "metal", "south field", "Keys");
        var baseline = service.CalculateScore(first, second);

        second.Category!.Name = "wallets";

        Assert.Equal(0, baseline);
        Assert.Equal(31.25m, service.CalculateScore(first, second));
    }

    [Fact]
    public void CalculateScore_ItemTextContributesItsNormalizedWeight()
    {
        var service = new LostItemSimilarityService();
        var first = Item("blue wallet", "leather", "north hall", "Wallets");
        var second = Item("red keys", "metal", "south field", "Keys");

        second.Title = first.Title;
        second.Description = first.Description;

        Assert.Equal(50m, service.CalculateScore(first, second));
    }

    [Fact]
    public void CalculateScore_LocationUsesCaseInsensitiveSubstringMatching()
    {
        var service = new LostItemSimilarityService();
        var first = Item("blue wallet", "leather", "Central Library, second floor", "Wallets");
        var second = Item("red keys", "metal", "central library", "Keys");

        Assert.Equal(18.75m, service.CalculateScore(first, second));

        second.Location = "   ";
        Assert.Equal(0m, service.CalculateScore(first, second));
    }

    [Fact]
    public void CalculateScore_VisibleFeaturesContributeToItemText()
    {
        var service = new LostItemSimilarityService();
        var first = Item("blue wallet", "leather", "north hall", "Wallets");
        var second = Item("red keys", "metal", "south field", "Keys");
        var baseline = service.CalculateScore(first, second);

        first.Features.Add(new LostItemFeature { Key = "Brand", Value = "Acme", IsHidden = false });
        second.Features.Add(new LostItemFeature { Key = "Brand", Value = "Acme", IsHidden = false });

        Assert.Equal(0m, baseline);
        Assert.True(service.CalculateScore(first, second) > baseline);
    }

    [Fact]
    public void CalculateScore_IgnoresHiddenFeatures()
    {
        var service = new LostItemSimilarityService();
        var first = Item("blue wallet", "leather", "north hall", "Wallets");
        var second = Item("red keys", "metal", "south field", "Keys");
        var baseline = service.CalculateScore(first, second);

        first.Features.Add(new LostItemFeature { Key = "Secret", Value = "exact secret", IsHidden = true });
        second.Features.Add(new LostItemFeature { Key = "Secret", Value = "exact secret", IsHidden = true });

        Assert.Equal(baseline, service.CalculateScore(first, second));
    }

    [Fact]
    public void CalculateScore_EventDateDoesNotAffectScore()
    {
        var service = new LostItemSimilarityService();
        var first = Item("blue wallet", "leather", "north hall", "Wallets");
        var second = Item("blue wallet", "leather", "north hall", "Wallets");
        var baseline = service.CalculateScore(first, second);

        first.EventDate = new DateTime(2020, 1, 1);
        second.EventDate = new DateTime(2030, 12, 31);

        Assert.Equal(baseline, service.CalculateScore(first, second));
    }

    [Fact]
    public void CalculateScore_RenormalizesAvailableComponentsAndHandlesMissingData()
    {
        var service = new LostItemSimilarityService();
        var noDataFirst = Item("", "", "", "");
        var noDataSecond = Item("", "", "", "");
        var categoryOnlyFirst = Item("", "", "", "Wallets");
        var categoryOnlySecond = Item("", "", "", "wallets");
        var partialFirst = Item("blue wallet", "", "", "Wallets");
        var partialSecond = Item("red keys", "", "", "Wallets");

        Assert.Equal(0m, service.CalculateScore(noDataFirst, noDataSecond));
        Assert.Equal(100m, service.CalculateScore(categoryOnlyFirst, categoryOnlySecond));
        Assert.Equal(38.46m, service.CalculateScore(partialFirst, partialSecond));
        Assert.InRange(service.CalculateScore(partialFirst, partialSecond), 0m, 100m);
    }

    private static LostItem Item(string title, string description, string location, string category) =>
        new()
        {
            Title = title,
            Description = description,
            Location = location,
            Category = new Category { Name = category }
        };
}
