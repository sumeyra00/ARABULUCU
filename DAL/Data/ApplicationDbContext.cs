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
        public DbSet<VehicleListing> VehicleListings { get; set; } = null!;
        public DbSet<VehicleFeature> VehicleFeatures { get; set; } = null!;
        public DbSet<ListingFeature> ListingFeatures { get; set; } = null!;

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

            modelBuilder.Entity<Notification>()
                .HasOne(notification => notification.User)
                .WithMany(user => user.Notifications)
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VehicleListing>()
                .HasOne(listing => listing.User)
                .WithMany(user => user.VehicleListings)
                .HasForeignKey(listing => listing.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ListingFeature>()
                .HasKey(listingFeature => new { listingFeature.VehicleListingId, listingFeature.VehicleFeatureId });

            modelBuilder.Entity<ListingFeature>()
                .HasOne(listingFeature => listingFeature.VehicleListing)
                .WithMany(listing => listing.Features)
                .HasForeignKey(listingFeature => listingFeature.VehicleListingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ListingFeature>()
                .HasOne(listingFeature => listingFeature.VehicleFeature)
                .WithMany(feature => feature.Listings)
                .HasForeignKey(listingFeature => listingFeature.VehicleFeatureId)
                .OnDelete(DeleteBehavior.Restrict);

            // Basit seed örneği: kampüste sık bulunan kategoriler
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Cüzdan" },
                new Category { Id = 2, Name = "Anahtar" },
                new Category { Id = 3, Name = "Telefon" },
                new Category { Id = 4, Name = "Kulaklık" }
            );

            modelBuilder.Entity<VehicleFeature>().HasData(
                new VehicleFeature { Id = 1, Name = "Sunroof", Description = "Açılır cam tavan", IsHiddenFeature = false },
                new VehicleFeature { Id = 2, Name = "Isıtmalı Koltuk", Description = "Ön veya arka koltuk ısıtma sistemi", IsHiddenFeature = false },
                new VehicleFeature { Id = 3, Name = "LED Far", Description = "LED ön far sistemi", IsHiddenFeature = false },
                new VehicleFeature { Id = 4, Name = "Geri Görüş Kamerası", Description = "Park manevralarını destekleyen kamera", IsHiddenFeature = false },
                new VehicleFeature { Id = 5, Name = "Kadran Selamlama", Description = "Gösterge paneli açılış animasyonu", IsHiddenFeature = true },
                new VehicleFeature { Id = 6, Name = "Gizli Menü Aktivasyonu", Description = "Araç bilgi-eğlence sistemindeki gizli menü", IsHiddenFeature = true },
                new VehicleFeature { Id = 7, Name = "Amerikan Park", Description = "Sinyal lambalarının park aydınlatması olarak kullanımı", IsHiddenFeature = true }
            );
        }
    }
}
