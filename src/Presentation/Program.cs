using Microsoft.EntityFrameworkCore;
using ARABULUCU.DAL.Data;
using ARABULUCU.DAL.Repositories;
using ARABULUCU.BLL.Services;
using ARABULUCU.ENT.Entities;
using ARABULUCU.Web.Hubs;
using ARABULUCU.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using System.Net.Sockets;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// MVC hizmetleri
builder.Services.AddControllersWithViews();
builder.Services.AddDataProtection().SetApplicationName("ARABULUCU");

// Doğrudan Supabase Session Pooler bağlantı dizesi
var connectionString = "Host=aws-0-eu-west-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.joxwercozcyuoumfueen;Password=Sümeyra123.";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));


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
builder.Services.AddScoped<ILostItemListingService, LostItemListingService>();
builder.Services.AddSingleton<ILostItemSimilarityService, LostItemSimilarityService>();

// SignalR hub kaydı (chat için)
builder.Services.AddSignalR();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await dbContext.Database.MigrateAsync();
    }
    catch (NpgsqlException exception) when (
        exception.GetBaseException() is SocketException socketException &&
        socketException.SocketErrorCode is SocketError.HostNotFound or SocketError.TryAgain or SocketError.NoData)
    {
        var host = new NpgsqlConnectionStringBuilder(connectionString).Host;
        throw new InvalidOperationException(
            $"PostgreSQL sunucusunun adresi çözümlenemedi: '{host}'. Supabase Dashboard > Connect > Session pooler adresini kullanın; doğrudan veritabanı adresi IPv6 gerektirebilir.",
            exception);
    }
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
