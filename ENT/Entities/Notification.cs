using System;

namespace ARABULUCU.ENT.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public UserProfile User { get; set; } = null!;
        public int? LostItemId { get; set; }
        public LostItem? LostItem { get; set; }
    }
}