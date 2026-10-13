using Microsoft.EntityFrameworkCore;
using ARABULUCU.DAL.Data;
using ARABULUCU.DAL.Repositories;
using ARABULUCU.ENT.Entities;
using ARABULUCU.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// MVC hizmetleri
builder.Services.AddControllersWithViews();

// DbContext: burada geliştirme için SQLite kullanalım. Connection string appsettings.json'dan alınabilir.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=arabulucu.db"));

// DAL repository kayıtları (IItemRepository kullanılıyor)
builder.Services.AddScoped<IItemRepository, ItemRepository>();

// SignalR hub kaydı (chat için)
builder.Services.AddSignalR();

var app = builder.Build();

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
