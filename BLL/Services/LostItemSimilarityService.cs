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
    private const double CategoryWeight = 31.25;
    private const double ItemTextWeight = 50;
    private const double LocationWeight = 18.75;

    // Available components are renormalized; the weights preserve the original ratios without event date.
    public decimal CalculateScore(LostItem first, LostItem second)
    {
        var weightedScore = 0d;
        var availableWeight = 0d;

        AddComponent(
            !string.IsNullOrWhiteSpace(first.Category?.Name) && !string.IsNullOrWhiteSpace(second.Category?.Name),
            string.Equals(first.Category?.Name, second.Category?.Name, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            CategoryWeight);

        var firstText = GetItemText(first);
        var secondText = GetItemText(second);
        AddComponent(!string.IsNullOrWhiteSpace(firstText) && !string.IsNullOrWhiteSpace(secondText),
            TokenSimilarity(firstText, secondText), ItemTextWeight);

        AddComponent(!string.IsNullOrWhiteSpace(first.Location) && !string.IsNullOrWhiteSpace(second.Location),
            LocationSimilarity(first.Location, second.Location), LocationWeight);

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

    private static double LocationSimilarity(string first, string second) =>
        first.Contains(second, StringComparison.OrdinalIgnoreCase) ||
        second.Contains(first, StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;
}
