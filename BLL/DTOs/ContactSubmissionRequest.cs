namespace ARABULUCU.BLL.DTOs;

public sealed record ContactSubmissionRequest(
    string Name,
    string Email,
    string Subject,
    string Message,
    string? UserId);
