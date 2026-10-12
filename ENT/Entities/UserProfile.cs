namespace ARABULUCU.ENT.Entities
{
    /// <summary>
    /// Basit kullanıcı profili; Identity kullanacaksanız IdentityUser genişletin veya ilişkilendirin.
    /// Bu sınıf uygulama içinde gösterim amaçlı bilgileri tutar.
    /// </summary>
    public class UserProfile
    {
        /// <summary>Kullanıcı Id (Identity ile eşlenebilir)</summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>Görünen ad</summary>
        public string DisplayName { get; set; } = string.Empty;
        /// <summary>Email adresi</summary>
        public string Email { get; set; } = string.Empty;
    }
}
