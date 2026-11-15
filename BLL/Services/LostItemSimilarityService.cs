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
    private const double CategoryWeight = 35;
    private const double LocationWeight = 15;
    private const double TitleWeight = 25;
    private const double DescriptionWeight = 25;
    private const double MaximumDateBonus = 10;
    private const double DateBonusDecreasePerDay = MaximumDateBonus / 7;

    public decimal CalculateScore(LostItem first, LostItem second)
    {
        var titleSimilarity = TokenSimilarity(first.Title, second.Title);
        var descriptionSimilarity = TokenSimilarity(first.Description, second.Description);
        if (titleSimilarity == 0 && descriptionSimilarity == 0)
        {
            return 0;
        }

        var score = 0d;
        if (!string.IsNullOrWhiteSpace(first.Category?.Name) &&
            !string.IsNullOrWhiteSpace(second.Category?.Name) &&
            string.Equals(first.Category.Name, second.Category.Name, StringComparison.OrdinalIgnoreCase))
        {
            score += CategoryWeight;
        }

        if (!string.IsNullOrWhiteSpace(first.Location) &&
            !string.IsNullOrWhiteSpace(second.Location) &&
            LocationMatches(first.Location, second.Location))
        {
            score += LocationWeight;
        }

        score += titleSimilarity * TitleWeight;
        score += descriptionSimilarity * DescriptionWeight;
        score += GetDateBonus(first.EventDate, second.EventDate);

        return decimal.Round((decimal)score, 2);
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

    private static bool LocationMatches(string first, string second) =>
        first.Contains(second, StringComparison.OrdinalIgnoreCase) ||
        second.Contains(first, StringComparison.OrdinalIgnoreCase);

    private static double GetDateBonus(DateTime first, DateTime second)
    {
        var dayDifference = Math.Abs((first.Date - second.Date).Days);
        return Math.Max(0, MaximumDateBonus - dayDifference * DateBonusDecreasePerDay);
    }
}
