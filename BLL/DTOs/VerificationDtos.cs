using ARABULUCU.ENT.Entities;

namespace ARABULUCU.BLL.DTOs;

public sealed class VerificationQuestionDto
{
    public int Id { get; init; }
    public string Prompt { get; init; } = string.Empty;
}

public sealed class MatchingCriteria
{
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public string? Color { get; init; }
    public DateTime EventDate { get; init; }
    public IReadOnlyCollection<VerificationAnswerDto> Answers { get; init; } = [];
}

public sealed class MatchingResult
{
    public bool IsSuccessful { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<LostItem> Items { get; init; } = [];
}

public sealed class VerificationAnswerDto
{
    public int QuestionId { get; init; }
    public string Answer { get; init; } = string.Empty;
}

public sealed class VerificationResult
{
    public bool IsSuccessful { get; init; }
    public string? Error { get; init; }
}

public sealed class ProtectedListingDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public IReadOnlyList<ProtectedFeatureDto> Features { get; init; } = [];
}

public sealed class ProtectedFeatureDto
{
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}
