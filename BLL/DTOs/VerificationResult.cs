namespace ARABULUCU.BLL.DTOs;

public sealed record VerificationResult(
    bool Succeeded,
    string? ErrorMessage = null,
    bool CanStartChat = false);
