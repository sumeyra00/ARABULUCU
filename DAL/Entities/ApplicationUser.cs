using Microsoft.AspNetCore.Identity;

namespace ARABULUCU.DAL.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = string.Empty;
    }
}
