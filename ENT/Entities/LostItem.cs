using System;
using System.Collections.Generic;

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
        public DateTime EventDate { get; set; } = DateTime.UtcNow;
        // IsFound describes the listing type (lost vs. found), not whether a lost item was recovered.
        public bool IsFound { get; set; } = false;
        public bool IsItemFound { get; set; } = false;
        public bool IsDelivered { get; set; } = false;

        public string UserId { get; set; } = string.Empty;
        public UserProfile User { get; set; } = null!;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public ICollection<LostItemFeature> Features { get; set; } = new List<LostItemFeature>();
    }
}
