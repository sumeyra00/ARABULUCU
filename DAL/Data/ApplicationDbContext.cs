using Microsoft.EntityFrameworkCore;
using ARABULUCU.ENT.Entities;

namespace ARABULUCU.DAL.Data
{
    // DAL katmanındaki DbContext: ENT içindeki entity sınıflarını kullanır.
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<LostItem> LostItems { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<LostItemFeature> LostItemFeatures { get; set; } = null!;
        public DbSet<ContactMessage> ContactMessages { get; set; } = null!;
        public DbSet<UserProfile> UserProfiles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Basit seed örneği: kampüste sık bulunan kategoriler
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Cüzdan" },
                new Category { Id = 2, Name = "Anahtar" },
                new Category { Id = 3, Name = "Telefon" },
                new Category { Id = 4, Name = "Kulaklık" }
            );
        }
    }
}
