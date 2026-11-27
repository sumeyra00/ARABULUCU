using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARABULUCU.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private const string AdminRoleName = "Admin";

    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(
        ApplicationDbContext dbContext,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? filter = "all", string? q = null)
    {
        var allItems = await _dbContext.LostItems
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.User)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

        var filteredItems = allItems.AsEnumerable();

        filteredItems = filter switch
        {
            "lost" => filteredItems.Where(item => !item.IsFound),
            "found" => filteredItems.Where(item => item.IsFound),
            "delivered" => filteredItems.Where(item => item.IsDelivered),
            "open" => filteredItems.Where(item => !item.IsDelivered && (item.IsFound || !item.IsItemFound)),
            _ => filteredItems
        };

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLowerInvariant();
            filteredItems = filteredItems.Where(item =>
                item.Title.ToLowerInvariant().Contains(term) ||
                item.Location.ToLowerInvariant().Contains(term) ||
                (item.Category?.Name.ToLowerInvariant().Contains(term) ?? false) ||
                (item.User?.Email?.ToLowerInvariant().Contains(term) ?? false));
        }

        var categories = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync();

        var submissions = await _dbContext.ContactSubmissions
            .AsNoTracking()
            .OrderByDescending(submission => submission.CreatedAt)
            .Take(50)
            .ToListAsync();

        var profiles = await _dbContext.UserProfiles
            .AsNoTracking()
            .OrderBy(profile => profile.DisplayName)
            .ToListAsync();

        var adminUsers = await _userManager.GetUsersInRoleAsync(AdminRoleName);
        var adminUserIds = adminUsers.Select(user => user.Id).ToHashSet(StringComparer.Ordinal);

        var userRows = profiles.Select(profile => new AdminUserRowViewModel
        {
            UserId = profile.Id,
            DisplayName = profile.DisplayName,
            Email = profile.Email,
            IsAdmin = adminUserIds.Contains(profile.Id)
        }).ToList();

        var totalChatMessagesCount = await _dbContext.ContactMessages.AsNoTracking().CountAsync();
        var totalSubmissionsCount = await _dbContext.ContactSubmissions.AsNoTracking().CountAsync();

        return View(new AdminDashboardViewModel
        {
            TotalItems = allItems.Count,
            LostItemsCount = allItems.Count(item => !item.IsFound),
            FoundItemsCount = allItems.Count(item => item.IsFound),
            DeliveredItemsCount = allItems.Count(item => item.IsDelivered),
            TotalUsersCount = profiles.Count,
            TotalChatMessagesCount = totalChatMessagesCount,
            TotalSubmissionsCount = totalSubmissionsCount,
            ActiveFilter = filter ?? "all",
            SearchQuery = q ?? string.Empty,
            Items = filteredItems.ToList(),
            Categories = categories,
            ContactSubmissions = submissions,
            Users = userRows
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDelivered(int id)
    {
        var item = await _dbContext.LostItems.SingleOrDefaultAsync(candidate => candidate.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        item.IsDelivered = !item.IsDelivered;
        if (item.IsDelivered)
        {
            item.IsItemFound = true;
        }

        await _dbContext.SaveChangesAsync();
        TempData["AdminSuccess"] = $"\"#{item.Id} - {item.Title}\" ilanının teslim durumu güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFoundStatus(int id)
    {
        var item = await _dbContext.LostItems.SingleOrDefaultAsync(candidate => candidate.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        item.IsItemFound = !item.IsItemFound;
        if (!item.IsItemFound)
        {
            item.IsDelivered = false;
        }

        await _dbContext.SaveChangesAsync();
        TempData["AdminSuccess"] = $"\"#{item.Id} - {item.Title}\" ilanının bulunma durumu güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditItem(AdminEditItemInputModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["AdminError"] = "İlan güncellenirken zorunlu alanları kontrol edin.";
            return RedirectToAction(nameof(Index));
        }

        var item = await _dbContext.LostItems.SingleOrDefaultAsync(candidate => candidate.Id == model.Id);
        if (item is null)
        {
            return NotFound();
        }

        var categoryExists = await _dbContext.Categories.AnyAsync(category => category.Id == model.CategoryId);
        if (!categoryExists)
        {
            TempData["AdminError"] = "Seçilen kategori bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        item.Title = model.Title.Trim();
        item.Description = model.Description.Trim();
        item.Location = model.Location.Trim();
        item.CategoryId = model.CategoryId;
        item.IsFound = model.IsFound;
        item.IsItemFound = model.IsDelivered || model.IsItemFound;
        item.IsDelivered = model.IsDelivered;

        await _dbContext.SaveChangesAsync();
        TempData["AdminSuccess"] = $"\"#{item.Id} - {item.Title}\" ilanı güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _dbContext.LostItems.SingleOrDefaultAsync(candidate => candidate.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var features = await _dbContext.LostItemFeatures
                .Where(feature => feature.LostItemId == id)
                .ToListAsync();
            if (features.Count > 0)
            {
                _dbContext.LostItemFeatures.RemoveRange(features);
            }

            var matches = await _dbContext.VerifiedItemMatches
                .Where(match => match.LostItemId == id || match.FoundItemId == id)
                .ToListAsync();
            if (matches.Count > 0)
            {
                _dbContext.VerifiedItemMatches.RemoveRange(matches);
            }

            var messages = await _dbContext.ContactMessages
                .Where(message => message.LostItemId == id || message.MatchedLostItemId == id)
                .ToListAsync();
            if (messages.Count > 0)
            {
                _dbContext.ContactMessages.RemoveRange(messages);
            }

            _dbContext.LostItems.Remove(item);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        TempData["AdminSuccess"] = $"\"#{item.Id} - {item.Title}\" ilanı sistemden silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(string? categoryName)
    {
        var trimmed = categoryName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > 100)
        {
            TempData["AdminError"] = "Geçerli bir kategori adı girin (1-100 karakter).";
            return RedirectToAction(nameof(Index));
        }

        var exists = await _dbContext.Categories
            .AnyAsync(category => category.Name.ToLower() == trimmed.ToLower());
        if (exists)
        {
            TempData["AdminError"] = $"\"{trimmed}\" kategorisi zaten mevcut.";
            return RedirectToAction(nameof(Index));
        }

        _dbContext.Categories.Add(new Category { Name = trimmed });
        await _dbContext.SaveChangesAsync();

        TempData["AdminSuccess"] = $"\"{trimmed}\" kategorisi eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _dbContext.Categories.SingleOrDefaultAsync(candidate => candidate.Id == id);
        if (category is null)
        {
            return NotFound();
        }

        var inUse = await _dbContext.LostItems.AnyAsync(item => item.CategoryId == id);
        if (inUse)
        {
            TempData["AdminError"] = $"\"{category.Name}\" kategorisine bağlı ilanlar bulunduğu için silinemedi.";
            return RedirectToAction(nameof(Index));
        }

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync();

        TempData["AdminSuccess"] = $"\"{category.Name}\" kategorisi silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSubmission(int id)
    {
        var submission = await _dbContext.ContactSubmissions.SingleOrDefaultAsync(candidate => candidate.Id == id);
        if (submission is null)
        {
            return NotFound();
        }

        _dbContext.ContactSubmissions.Remove(submission);
        await _dbContext.SaveChangesAsync();
        TempData["AdminSuccess"] = "İletişim mesajı kaydı silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdminRole(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        if (!await _roleManager.RoleExistsAsync(AdminRoleName))
        {
            await _roleManager.CreateAsync(new IdentityRole(AdminRoleName));
        }

        var currentUserId = _userManager.GetUserId(User);
        var isCurrentlyAdmin = await _userManager.IsInRoleAsync(user, AdminRoleName);

        if (isCurrentlyAdmin)
        {
            if (currentUserId == user.Id)
            {
                TempData["AdminError"] = "Kendi hesabınızdan Admin rolünü kaldıramazsınız.";
                return RedirectToAction(nameof(Index));
            }

            await _userManager.RemoveFromRoleAsync(user, AdminRoleName);
            TempData["AdminSuccess"] = $"\"{user.Email}\" kullanıcısından Admin rolü kaldırıldı.";
        }
        else
        {
            await _userManager.AddToRoleAsync(user, AdminRoleName);
            TempData["AdminSuccess"] = $"\"{user.Email}\" kullanıcısına Admin rolü verildi.";
        }

        return RedirectToAction(nameof(Index));
    }
}

public sealed class AdminEditItemInputModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public bool IsFound { get; set; }
    public bool IsItemFound { get; set; }
    public bool IsDelivered { get; set; }
}

public sealed class AdminUserRowViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}

public sealed class AdminDashboardViewModel
{
    public int TotalItems { get; set; }
    public int LostItemsCount { get; set; }
    public int FoundItemsCount { get; set; }
    public int DeliveredItemsCount { get; set; }
    public int TotalUsersCount { get; set; }
    public int TotalChatMessagesCount { get; set; }
    public int TotalSubmissionsCount { get; set; }
    public string ActiveFilter { get; set; } = "all";
    public string SearchQuery { get; set; } = string.Empty;
    public List<LostItem> Items { get; set; } = [];
    public List<Category> Categories { get; set; } = [];
    public List<ContactSubmission> ContactSubmissions { get; set; } = [];
    public List<AdminUserRowViewModel> Users { get; set; } = [];
}