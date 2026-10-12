using System;

namespace ARABULUCU.ENT.Entities
{
    /// <summary>
    /// Kayıp eşya ilanını temsil eden POCO sınıfı.
    /// Basit özellikler: başlık, açıklama, konum, resim yolu ve oluşturulma zamanı.
    /// </summary>
    public class LostItem
    {
        /// <summary>Birincil anahtar</summary>
        public int Id { get; set; }
        /// <summary>İlan başlığı</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Detaylı açıklama</summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>Resim dosyası yolu veya URL (isteğe bağlı)</summary>
        public string? ImagePath { get; set; }
        /// <summary>Bulunduğu yer veya açıklama</summary>
        public string Location { get; set; } = string.Empty; // serbest metin veya koordinat bilgisi
        /// <summary>İlan oluşturulma zamanı (UTC)</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        /// <summary>İlanın bulunup bulunmadığı</summary>
        public bool IsFound { get; set; } = false;
    }
}
