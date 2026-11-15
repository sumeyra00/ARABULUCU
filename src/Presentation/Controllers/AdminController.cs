using System.Security.Claims;
using ARABULUCU.DAL.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARABULUCU.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public AdminController(ApplicationDbContext dbContext) => _dbContext = dbContext;

    [HttpGet]
    public async Task<IActionResult> Reports()
    {
        var reports = await _dbContext.ListingReports
            .AsNoTracking()
            .Include(report => report.LostItem)
            .Where(report => report.Status == "Pending")
            .OrderBy(report => report.CreatedAt)
            .Select(report => new ModerationReportViewModel
            {
                Id = report.Id,
                LostItemId = report.LostItemId,
                Title = report.LostItem.Title,
                Reason = report.Reason,
                Details = report.Details,
                CreatedAt = report.CreatedAt
            })
            .ToListAsync();

        return View("~/Views/Admin/Reports.cshtml", reports);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewReport(int id, string decision, string? note)
    {
        var report = await _dbContext.ListingReports
            .Include(value => value.LostItem)
            .SingleOrDefaultAsync(value => value.Id == id);
        if (report is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        report.Status = decision == "Remove" ? "Removed" : "Rejected";
        report.ReviewedAt = DateTime.UtcNow;
        report.ReviewedByUserId = userId;
        report.ResolutionNote = note?.Trim();
        report.LostItem.LastActivityAt = DateTime.UtcNow;

        if (decision == "Remove")
        {
            report.LostItem.IsArchived = true;
            _dbContext.ListingStatusHistories.Add(new ENT.Entities.ListingStatusHistory
            {
                LostItemId = report.LostItemId,
                Status = "Moderated",
                ChangedByUserId = userId,
                Details = note?.Trim() ?? "İlan moderasyon sonucunda arşivlendi."
            });
        }

        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Reports));
    }
}

public sealed class ModerationReportViewModel
{
    public int Id { get; set; }
    public int LostItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
