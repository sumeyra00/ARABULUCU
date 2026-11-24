using System.Security.Claims;
using ARABULUCU.BLL.Services;
using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ARABULUCU.Web.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    private const string ConversationKey = "ChatHub.Conversation";
    private readonly ApplicationDbContext _dbContext;
    private readonly ILostItemListingService _listingService;

    public ChatHub(ApplicationDbContext dbContext, ILostItemListingService listingService)
    {
        _dbContext = dbContext;
        _listingService = listingService;
    }

    public async Task<IReadOnlyList<ChatMessageDto>> JoinConversation(int itemId, int matchedItemId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null ||
            !await _listingService.CanChatAsync(userId, itemId, matchedItemId))
        {
            throw new HubException("Bu ilan çifti için sohbet eşleşmesi bulunamadı.");
        }

        var profile = await _dbContext.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId);
        if (profile is null)
        {
            throw new HubException("Sohbete katılmak için kullanıcı profili bulunamadı.");
        }

        if (Context.Items.TryGetValue(ConversationKey, out var current) &&
            current is Conversation previous)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(previous));
        }

        var pair = OrderPair(itemId, matchedItemId);
        var conversation = new Conversation(pair.ItemId, pair.MatchedItemId, userId, false, userId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(conversation));
        Context.Items[ConversationKey] = conversation;

        // İlan çifti sırası (A,B) veya (B,A) fark etmeksizin tüm sohbet mesajlarını getir.
        return await _dbContext.ContactMessages
            .AsNoTracking()
            .Where(message =>
                message.MatchedLostItemId != null &&
                ((message.LostItemId == itemId && message.MatchedLostItemId == matchedItemId) ||
                 (message.LostItemId == matchedItemId && message.MatchedLostItemId == itemId)))
            .OrderBy(message => message.SentAt)
            .ThenBy(message => message.Id)
            .Take(200)
            .Select(message => new ChatMessageDto(
                message.Id,
                message.SenderUserId ?? string.Empty,
                message.FromUserId,
                message.Message,
                message.SentAt))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ChatMessageDto>> JoinVerifiedConversation(int foundItemId, string participantUserId)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null ||
            !await _listingService.CanChatWithVerifiedItemAsync(userId, foundItemId, participantUserId))
        {
            throw new HubException("Bu doğrulanmış sohbet için yetkiniz bulunmuyor.");
        }

        if (Context.Items.TryGetValue(ConversationKey, out var current) &&
            current is Conversation previous)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(previous));
        }

        var conversation = new Conversation(foundItemId, null, userId, true, participantUserId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(conversation));
        Context.Items[ConversationKey] = conversation;

        var foundItemOwnerId = await _dbContext.LostItems
            .AsNoTracking()
            .Where(item => item.Id == foundItemId)
            .Select(item => item.UserId)
            .SingleOrDefaultAsync();

        var allowedSenders = new[] { userId, participantUserId, foundItemOwnerId }
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToArray();

        return await _dbContext.ContactMessages
            .AsNoTracking()
            .Where(message =>
                message.LostItemId == foundItemId &&
                message.MatchedLostItemId == null &&
                message.SenderUserId != null &&
                allowedSenders.Contains(message.SenderUserId))
            .OrderBy(message => message.SentAt)
            .ThenBy(message => message.Id)
            .Select(message => new ChatMessageDto(
                message.Id,
                message.SenderUserId ?? string.Empty,
                message.FromUserId,
                message.Message,
                message.SentAt))
            .ToListAsync();
    }

    public async Task SendMessage(string? message)
    {
        if (!Context.Items.TryGetValue(ConversationKey, out var current) ||
            current is not Conversation conversation)
        {
            throw new HubException("Mesaj göndermeden önce geçerli bir eşleşme sohbetine katılmalısınız.");
        }

        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null ||
            (conversation.IsVerified
                ? !await _listingService.CanChatWithVerifiedItemAsync(userId, conversation.ItemId, conversation.ParticipantUserId)
                : !await _listingService.CanChatAsync(userId, conversation.ItemId, conversation.MatchedItemId!.Value)))
        {
            throw new HubException("Bu eşleşme için sohbet yetkiniz bulunmuyor.");
        }

        var content = message?.Trim();
        if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
        {
            throw new HubException("Mesaj 1-2000 karakter arasında olmalı.");
        }

        var displayName = await _dbContext.UserProfiles
            .AsNoTracking()
            .Where(profile => profile.Id == userId)
            .Select(profile => profile.DisplayName)
            .SingleOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new HubException("Sohbete katılmak için kullanıcı profili bulunamadı.");
        }

        var chatMessage = new ContactMessage
        {
            LostItemId = conversation.ItemId,
            MatchedLostItemId = conversation.IsVerified ? null : conversation.MatchedItemId,
            SenderUserId = userId,
            FromUserId = displayName,
            Message = content,
            SentAt = DateTime.UtcNow
        };

        _dbContext.ContactMessages.Add(chatMessage);
        await _dbContext.SaveChangesAsync();

        await Clients.Group(GetGroupName(conversation)).SendAsync(
            "ReceiveMessage",
            new ChatMessageDto(chatMessage.Id, userId, chatMessage.FromUserId, chatMessage.Message, chatMessage.SentAt));
    }

    private static string GetGroupName(Conversation conversation) =>
        conversation.IsVerified
            ? $"verified-item-{conversation.ItemId}-{conversation.ParticipantUserId}"
            : $"items-{conversation.ItemId}-{conversation.MatchedItemId}";

    private static (int ItemId, int MatchedItemId) OrderPair(int itemId, int matchedItemId) =>
        itemId < matchedItemId ? (itemId, matchedItemId) : (matchedItemId, itemId);

    private sealed record Conversation(
        int ItemId,
        int? MatchedItemId,
        string UserId,
        bool IsVerified,
        string ParticipantUserId);
}

public sealed record ChatMessageDto(int Id, string SenderUserId, string Sender, string Message, DateTime SentAt);