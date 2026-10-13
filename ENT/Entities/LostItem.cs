using System;

namespace ARABULUCU.ENT.Entities
{
    // ENT katmanı POCO sınıfları: Kayıp eşya ilanı
    public class LostItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public string Location { get; set; } = string.Empty; // serbest metin veya koordinat bilgisi
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsFound { get; set; } = false;
    }
}
