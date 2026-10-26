    using ARABULUCU.DAL.Data;
    using ARABULUCU.ENT.Entities;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.EntityFrameworkCore;

    namespace ARABULUCU.Web.Hubs
    {
        public class ChatHub : Hub
        {
            private const string ConversationKey = "ChatHub.Conversation";
            private readonly ApplicationDbContext _dbContext;

            public ChatHub(ApplicationDbContext dbContext)
            {
                _dbContext = dbContext;
            }

            public async Task<IReadOnlyList<ChatMessageDto>> JoinConversation(int itemId, string? displayName)
            {
                if (itemId <= 0)
                {
                    throw new HubException("Geçerli bir ilan numarası gerekli.");
                }

                var isAuthenticated = Context.User?.Identity?.IsAuthenticated == true;
                var senderName = (isAuthenticated ? Context.User?.Identity?.Name : displayName)?.Trim();
                if (string.IsNullOrWhiteSpace(senderName) || senderName.Length > 50)
                {
                    throw new HubException("Görünen ad 1-50 karakter arasında olmalı.");
                }

                if (Context.Items.TryGetValue(ConversationKey, out var current)
                    && current is Conversation previous)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(previous.ItemId));
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(itemId));
                Context.Items[ConversationKey] = new Conversation(itemId, senderName);

                return await _dbContext.ContactMessages
                    .AsNoTracking()
                    .Where(message => message.LostItemId == itemId)
                    .OrderByDescending(message => message.SentAt)
                    .ThenByDescending(message => message.Id)
                    .Take(100)
                    .OrderBy(message => message.SentAt)
                    .ThenBy(message => message.Id)
                    .Select(message => new ChatMessageDto(
                        message.Id,
                        message.FromUserId,
                        message.Message,
                        message.SentAt))
                    .ToListAsync();
            }

            public async Task SendMessage(string? message)
            {
                if (!Context.Items.TryGetValue(ConversationKey, out var current)
                    || current is not Conversation conversation)
                {
                    throw new HubException("Mesaj göndermeden önce bir sohbete katılmalısınız.");
                }

                var content = message?.Trim();
                if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
                {
                    throw new HubException("Mesaj 1-2000 karakter arasında olmalı.");
                }

                var chatMessage = new ContactMessage
                {
                    LostItemId = conversation.ItemId,
                    FromUserId = conversation.DisplayName,
                    Message = content,
                    SentAt = DateTime.UtcNow
                };

                _dbContext.ContactMessages.Add(chatMessage);
                await _dbContext.SaveChangesAsync();

                await Clients.Group(GetGroupName(conversation.ItemId)).SendAsync(
                    "ReceiveMessage",
                    new ChatMessageDto(chatMessage.Id, chatMessage.FromUserId, chatMessage.Message, chatMessage.SentAt));
            }

            private static string GetGroupName(int itemId) => $"item-{itemId}";

            private sealed record Conversation(int ItemId, string DisplayName);
        }

        public sealed record ChatMessageDto(int Id, string Sender, string Message, DateTime SentAt);
}
