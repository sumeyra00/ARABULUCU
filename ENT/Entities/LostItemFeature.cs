namespace ARABULUCU.ENT.Entities
{
    /// <summary>
    /// İlan özellikleri (anahtar/değer). IsHidden ile ilan sahibi bilgiyi gizleyebilir.
    /// Örneğin: Renk=Kırmızı
    /// </summary>
    public class LostItemFeature
    {
        /// <summary>Birincil anahtar</summary>
        public int Id { get; set; }
        /// <summary>Bu özelliğin ait olduğu LostItem Id</summary>
        public int LostItemId { get; set; }
        /// <summary>Özellik anahtarı</summary>
        public string Key { get; set; } = string.Empty;
        /// <summary>Özellik değeri</summary>
        public string Value { get; set; } = string.Empty;
        /// <summary>İlan sahibi bu özelliği gizlemek isterse true olur</summary>
        public bool IsHidden { get; set; } = false;
    }
}
