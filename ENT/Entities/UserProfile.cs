using System.Collections.Generic;

namespace ARABULUCU.ENT.Entities
{
    // Basit kullanıcı profili; Identity kullanacaksanız IdentityUser genişletin veya ilişkilendirin.
    public class UserProfile
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public ICollection<LostItem> LostItems { get; set; } = new List<LostItem>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<VehicleListing> VehicleListings { get; set; } = new List<VehicleListing>();
    }
}
