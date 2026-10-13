namespace ARABULUCU.ENT.Entities
{
    // İlan özellikleri (anahtar/değer). IsHidden ile ilan sahibi bilgiyi gizleyebilir.
    public class LostItemFeature
    {
        public int Id { get; set; }
        public int LostItemId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsHidden { get; set; } = false;
    }
}
