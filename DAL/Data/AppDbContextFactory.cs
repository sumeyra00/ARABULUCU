using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ARABULUCU.DAL.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Web katmanındaki appsettings.json dosyanızdaki SQLite bağlantı yolunu buraya veriyoruz
            optionsBuilder.UseSqlite("Data Source=arabulucu.db"); // veya arkadaşlarınızın kullandığı connection string neyse

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}