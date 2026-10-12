namespace ARABULUCU.ENT.Entities
{
    /// <summary>
    /// Eşya kategorisi (ör: Cüzdan, Telefon).
    /// Kategoriler DbContext seed ile başlangıçta eklenir.
    /// </summary>
    public class Category
    {
        /// <summary>Birincil anahtar</summary>
        public int Id { get; set; }
        /// <summary>Kategori adı</summary>
        public string Name { get; set; } = string.Empty;
    }
}
