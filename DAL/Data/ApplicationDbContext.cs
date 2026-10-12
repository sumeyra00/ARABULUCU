using Microsoft.EntityFrameworkCore;
using ARABULUCU.ENT.Entities;

namespace ARABULUCU.DAL.Data
{
    /// <summary>
    /// Entity Framework Core DbContext: veritabanı tablolarını ve seed verilerini tanımlar.
    /// Bu context ENT katmanındaki POCO sınıflarını kullanır.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        /// <summary>Kayıp eşya ilanları tablosu</summary>
        public DbSet<LostItem> LostItems { get; set; } = null!;
        /// <summary>Kategori tablosu (ör. Cüzdan, Telefon)</summary>
        public DbSet<Category> Categories { get; set; } = null!;
        /// <summary>İlan özellikleri (anahtar/değer)</summary>
        public DbSet<LostItemFeature> LostItemFeatures { get; set; } = null!;
        /// <summary>İlanlarla ilişkili iletişim mesajları</summary>
        public DbSet<ContactMessage> ContactMessages { get; set; } = null!;
        /// <summary>Kullanıcı profilleri</summary>
        public DbSet<UserProfile> UserProfiles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Basit seed örneği: uygulama ilk çalıştığında tabloya eklenecek kategoriler
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Cüzdan" },
                new Category { Id = 2, Name = "Anahtar" },
                new Category { Id = 3, Name = "Telefon" },
                new Category { Id = 4, Name = "Kulaklık" }
            );
        }
    }
}
