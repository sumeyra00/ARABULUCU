using Microsoft.EntityFrameworkCore;
using ARABULUCU.DAL.Data;
using ARABULUCU.DAL.Repositories;
using ARABULUCU.BLL.Services;
using ARABULUCU.ENT.Entities;
using ARABULUCU.Web.Hubs;
using ARABULUCU.Web.Data;
using ARABULUCU.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// MVC hizmetleri
builder.Services.AddControllersWithViews();
builder.Services.AddDataProtection().SetApplicationName("ARABULUCU");

// DbContext: burada geliştirme için SQLite kullanalım. Connection string appsettings.json'dan alınabilir.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=arabulucu.db"));

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddTransient<IAccountEmailSender, SmtpAccountEmailSender>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});

// DAL repository kayıtları (IItemRepository kullanılıyor)
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IVehicleListingService, VehicleListingService>();
builder.Services.AddScoped<ILostItemListingService, LostItemListingService>();
builder.Services.AddSingleton<ILostItemSimilarityService, LostItemSimilarityService>();

// SignalR hub kaydı (chat için)
builder.Services.AddSignalR();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SqliteDatabaseInitializer.InitializeAsync(dbContext);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SignalR hub endpoint'i
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
