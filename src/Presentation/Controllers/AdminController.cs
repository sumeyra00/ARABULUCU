using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARABULUCU.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public AdminController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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

        var submissions = await _dbContext.ContactSubmissions
            .AsNoTracking()
            .OrderByDescending(submission => submission.CreatedAt)
            .Take(30)
            .ToListAsync();

        var totalUsersCount = await _dbContext.UserProfiles.AsNoTracking().CountAsync();
        var totalChatMessagesCount = await _dbContext.ContactMessages.AsNoTracking().CountAsync();
        var totalSubmissionsCount = await _dbContext.ContactSubmissions.AsNoTracking().CountAsync();

        return View(new AdminDashboardViewModel
        {
            TotalItems = allItems.Count,
            LostItemsCount = allItems.Count(item => !item.IsFound),
            FoundItemsCount = allItems.Count(item => item.IsFound),
            DeliveredItemsCount = allItems.Count(item => item.IsDelivered),
            TotalUsersCount = totalUsersCount,
            TotalChatMessagesCount = totalChatMessagesCount,
            TotalSubmissionsCount = totalSubmissionsCount,
            ActiveFilter = filter ?? "all",
            SearchQuery = q ?? string.Empty,
            Items = filteredItems.ToList(),
            ContactSubmissions = submissions
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
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _dbContext.LostItems.SingleOrDefaultAsync(candidate => candidate.Id == id);
        if (item is null)
        {
            return NotFound();
        }

        _dbContext.LostItems.Remove(item);
        await _dbContext.SaveChangesAsync();
        TempData["AdminSuccess"] = $"\"#{item.Id} - {item.Title}\" ilanı sistemden silindi.";
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
    public List<ContactSubmission> ContactSubmissions { get; set; } = [];
}