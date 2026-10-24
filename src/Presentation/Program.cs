using Microsoft.EntityFrameworkCore;
using ARABULUCU.DAL.Data;
using ARABULUCU.DAL.Entities;
using ARABULUCU.DAL.Repositories;
using ARABULUCU.ENT.Entities;
using ARABULUCU.Web.Hubs;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// MVC hizmetleri
builder.Services.AddControllersWithViews();

// DbContext: burada geliştirme için SQLite kullanalım. Connection string appsettings.json'dan alınabilir.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=arabulucu.db"));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    options.User.RequireUniqueEmail = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// DAL repository kayıtları (IItemRepository kullanılıyor)
builder.Services.AddScoped<IItemRepository, ItemRepository>();

// SignalR hub kaydı (chat için)
builder.Services.AddSignalR();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await RecordLegacyDatabaseBaselineAsync(dbContext);
    await dbContext.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SignalR hub endpoint'i
app.MapHub<ChatHub>("/hubs/chat");

app.Run();

static async Task RecordLegacyDatabaseBaselineAsync(ApplicationDbContext dbContext)
{
    if ((await dbContext.Database.GetAppliedMigrationsAsync()).Any())
    {
        return;
    }

    var migrations = dbContext.Database.GetMigrations().ToArray();
    if (migrations.Length == 0)
    {
        return;
    }

    var legacyTableCount = await dbContext.Database.SqlQueryRaw<int>(
        """
        SELECT COUNT(*) AS Value
        FROM sqlite_master
        WHERE type = 'table'
          AND name IN ('Categories', 'ContactMessages', 'LostItemFeatures',
                       'LostItems', 'Notifications', 'UserProfiles')
        """)
        .SingleAsync();

    if (legacyTableCount != 6)
    {
        return;
    }

    var productVersion = typeof(DbContext).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion.Split('+')[0] ?? "10.0.0";

    // EnsureCreated databases already contain this baseline schema, so record it before applying the Identity migration.
    await dbContext.Database.ExecuteSqlRawAsync(
        """
        CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
            "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
            "ProductVersion" TEXT NOT NULL
        )
        """);

    await dbContext.Database.ExecuteSqlInterpolatedAsync(
        $"INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({migrations[0]}, {productVersion})");
}
