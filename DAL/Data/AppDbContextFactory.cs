using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ARABULUCU.DAL.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            var searchDirectories = new[]
            {
                new DirectoryInfo(Directory.GetCurrentDirectory()),
                new DirectoryInfo(AppContext.BaseDirectory)
            };

            string? appSettingsPath = null;
            foreach (var startDirectory in searchDirectories)
            {
                var directory = startDirectory;
                while (directory is not null)
                {
                    var candidate = Path.Combine(
                        directory.FullName,
                        "src",
                        "Presentation",
                        "appsettings.json");

                    if (File.Exists(candidate))
                    {
                        appSettingsPath = candidate;
                        break;
                    }

                    directory = directory.Parent;
                }

                if (appSettingsPath is not null)
                {
                    break;
                }
            }

            var configurationBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables();

            if (appSettingsPath is not null)
            {
                configurationBuilder.AddJsonFile(appSettingsPath, optional: false);
            }

            var configuration = configurationBuilder.Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection appsettings.json veya ortam değişkeninde ayarlanmamış.");
            }

            optionsBuilder.UseNpgsql(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}