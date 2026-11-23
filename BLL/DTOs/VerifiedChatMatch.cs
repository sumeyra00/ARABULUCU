using ARABULUCU.ENT.Entities;

namespace ARABULUCU.BLL.DTOs;

public sealed class VerifiedChatMatch
{
    public required LostItem FoundItem { get; init; }
    public required string ParticipantUserId { get; init; }
    public required string ParticipantDisplayName { get; init; }
}
