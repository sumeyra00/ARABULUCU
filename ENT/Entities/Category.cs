using System.Collections.Generic;

namespace ARABULUCU.ENT.Entities
{
    // Eşya kategorisi (ör: cüzdan, telefon)
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<LostItem> LostItems { get; set; } = new List<LostItem>();
    }
}
