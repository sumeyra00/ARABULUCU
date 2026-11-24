using System.Globalization;
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

        var firstCategory = NormalizeText(first.Category?.Name);
        var secondCategory = NormalizeText(second.Category?.Name);
        AddComponent(
            !string.IsNullOrWhiteSpace(firstCategory) && !string.IsNullOrWhiteSpace(secondCategory),
            CategorySimilarity(firstCategory, secondCategory),
            CategoryWeight);

        var firstText = NormalizeText(GetItemText(first));
        var secondText = NormalizeText(GetItemText(second));
        AddComponent(
            !string.IsNullOrWhiteSpace(firstText) && !string.IsNullOrWhiteSpace(secondText),
            TokenSimilarity(firstText, secondText),
            ItemTextWeight);

        var firstLocation = NormalizeText(first.Location);
        var secondLocation = NormalizeText(second.Location);
        AddComponent(
            !string.IsNullOrWhiteSpace(firstLocation) && !string.IsNullOrWhiteSpace(secondLocation),
            LocationSimilarity(firstLocation, secondLocation),
            LocationWeight);

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

    private static double CategorySimilarity(string first, string second)
    {
        if (string.Equals(first, second, StringComparison.Ordinal))
        {
            return 1;
        }

        var firstTokens = TokenPattern.Matches(first).Select(m => m.Value).ToHashSet(StringComparer.Ordinal);
        var secondTokens = TokenPattern.Matches(second).Select(m => m.Value).ToHashSet(StringComparer.Ordinal);
        if (firstTokens.Count == 0 || secondTokens.Count == 0)
        {
            return 0;
        }

        var intersection = firstTokens.Count(secondTokens.Contains);
        return intersection > 0 ? 0.75 : 0;
    }

    private static double TokenSimilarity(string first, string second)
    {
        var firstTokens = TokenPattern.Matches(first)
            .Select(match => match.Value)
            .ToHashSet(StringComparer.Ordinal);
        var secondTokens = TokenPattern.Matches(second)
            .Select(match => match.Value)
            .ToHashSet(StringComparer.Ordinal);
        if (firstTokens.Count == 0 || secondTokens.Count == 0)
        {
            return 0;
        }

        var intersection = firstTokens.Count(secondTokens.Contains);
        var union = firstTokens.Union(secondTokens, StringComparer.Ordinal).Count();
        return union == 0 ? 0 : (double)intersection / union;
    }

    private static double LocationSimilarity(string first, string second)
    {
        if (first.Contains(second, StringComparison.Ordinal) ||
            second.Contains(first, StringComparison.Ordinal))
        {
            return 1;
        }

        var firstTokens = TokenPattern.Matches(first).Select(m => m.Value).ToHashSet(StringComparer.Ordinal);
        var secondTokens = TokenPattern.Matches(second).Select(m => m.Value).ToHashSet(StringComparer.Ordinal);
        if (firstTokens.Count == 0 || secondTokens.Count == 0)
        {
            return 0;
        }

        var intersection = firstTokens.Count(secondTokens.Contains);
        var minCount = Math.Min(firstTokens.Count, secondTokens.Count);
        return minCount == 0 ? 0 : (double)intersection / minCount;
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trCulture = new CultureInfo("tr-TR");
        var lower = value.Trim().ToLower(trCulture);

        var mapped = lower
            .Replace('ı', 'i')
            .Replace('İ', 'i')
            .Replace('ğ', 'g')
            .Replace('ü', 'u')
            .Replace('ş', 's')
            .Replace('ö', 'o')
            .Replace('ç', 'c');

        var parts = mapped.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts);
    }
}