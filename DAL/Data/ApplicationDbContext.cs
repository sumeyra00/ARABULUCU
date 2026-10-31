using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ARABULUCU.ENT.Entities;

namespace ARABULUCU.DAL.Data
{
    // DAL katmanındaki DbContext: ENT içindeki entity sınıflarını kullanır.
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<LostItem> LostItems { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<LostItemFeature> LostItemFeatures { get; set; } = null!;
        public DbSet<ContactMessage> ContactMessages { get; set; } = null!;
        public DbSet<UserProfile> UserProfiles { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LostItem>()
                .HasOne(item => item.User)
                .WithMany(user => user.LostItems)
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LostItem>()
                .HasOne(item => item.Category)
                .WithMany(category => category.LostItems)
                .HasForeignKey(item => item.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LostItem>()
                .Property(item => item.EventDate)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<LostItem>()
                .HasMany(item => item.Features)
                .WithOne()
                .HasForeignKey(feature => feature.LostItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(notification => notification.User)
                .WithMany(user => user.Notifications)
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.Cascade);

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
