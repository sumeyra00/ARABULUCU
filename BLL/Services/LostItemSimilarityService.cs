using System.Text.RegularExpressions;
using ARABULUCU.ENT.Entities;

namespace ARABULUCU.BLL.Services;

public interface ILostItemSimilarityService
{
    decimal CalculateScore(LostItem first, LostItem second);
}

public sealed class LostItemSimilarityService : ILostItemSimilarityService
{
    private static readonly Regex TokenPattern = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);

    // Available components are renormalized: category 25%, item text 40%, location 15%, event date 20%.
    public decimal CalculateScore(LostItem first, LostItem second)
    {
        var weightedScore = 0d;
        var availableWeight = 0d;

        AddComponent(
            !string.IsNullOrWhiteSpace(first.Category?.Name) && !string.IsNullOrWhiteSpace(second.Category?.Name),
            string.Equals(first.Category?.Name, second.Category?.Name, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            25);

        var firstText = GetItemText(first);
        var secondText = GetItemText(second);
        AddComponent(!string.IsNullOrWhiteSpace(firstText) && !string.IsNullOrWhiteSpace(secondText),
            TokenSimilarity(firstText, secondText), 40);

        AddComponent(!string.IsNullOrWhiteSpace(first.Location) && !string.IsNullOrWhiteSpace(second.Location),
            TokenSimilarity(first.Location, second.Location), 15);

        AddComponent(first.EventDate != default && second.EventDate != default,
            DateSimilarity(first.EventDate, second.EventDate), 20);

        if (availableWeight == 0)
        {
            return 0;
        }

        return decimal.Round((decimal)Math.Clamp(weightedScore / availableWeight * 100, 0, 100), 2);

        void AddComponent(bool available, double score, double weight)
        {
            if (!available)
            {
                return;
            }

            weightedScore += Math.Clamp(score, 0, 1) * weight;
            availableWeight += weight;
        }
    }

    private static string GetItemText(LostItem item)
    {
        var visibleFeatures = item.Features
            .Where(feature => !feature.IsHidden)
            .Select(feature => $"{feature.Key} {feature.Value}");
        return string.Join(' ', new[] { item.Title, item.Description }.Concat(visibleFeatures));
    }

    private static double TokenSimilarity(string first, string second)
    {
        var firstTokens = TokenPattern.Matches(first)
            .Select(match => match.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var secondTokens = TokenPattern.Matches(second)
            .Select(match => match.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (firstTokens.Count == 0 || secondTokens.Count == 0)
        {
            return 0;
        }

        var intersection = firstTokens.Count(secondTokens.Contains);
        var union = firstTokens.Union(secondTokens, StringComparer.OrdinalIgnoreCase).Count();
        return (double)intersection / union;
    }

    private static double DateSimilarity(DateTime first, DateTime second)
    {
        // Event dates lose one point for each day apart and contribute nothing after 30 days.
        var daysApart = Math.Abs((first - second).TotalDays);
        return Math.Max(0, 1 - daysApart / 30);
    }
}
