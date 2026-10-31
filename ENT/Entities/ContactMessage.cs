using System;

namespace ARABULUCU.ENT.Entities
{
    // Kullanıcılar arasındaki iletişim mesajı (chat veya in-app mesaj)
    public class ContactMessage
    {
        public int Id { get; set; }
        public int LostItemId { get; set; }
        public int? MatchedLostItemId { get; set; }
        public string? SenderUserId { get; set; }
        public string FromUserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
