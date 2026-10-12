using System;

namespace ARABULUCU.ENT.Entities
{
    /// <summary>
    /// Kullanıcılar arasındaki iletişim mesajı (chat veya uygulama içi mesaj).
    /// LostItem ile ilişkilidir.
    /// </summary>
    public class ContactMessage
    {
        /// <summary>Birincil anahtar</summary>
        public int Id { get; set; }
        /// <summary>İlgili ilan Id</summary>
        public int LostItemId { get; set; }
        /// <summary>Gönderen kullanıcı Id</summary>
        public string FromUserId { get; set; } = string.Empty;
        /// <summary>Mesaj içeriği</summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>Gönderim zamanı (UTC)</summary>
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
