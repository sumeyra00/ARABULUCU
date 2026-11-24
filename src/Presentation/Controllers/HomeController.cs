using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ARABULUCU.BLL.DTOs;
using ARABULUCU.BLL.Services;
using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARABULUCU.Web.Controllers;

public class HomeController : Controller
{
    private static readonly IReadOnlyDictionary<string, string> CategoryNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["canta"] = "Çanta & Sırt Çantası",
            ["anahtar"] = "Anahtar & Aksesuar",
            ["cuzdan"] = "Cüzdan & Kartlık / Kimlik",
            ["elektronik"] = "Kulaklık & Elektronik",
            ["telefon"] = "Telefon & Şarj Aleti",
            ["saat"] = "Akıllı Saat & Takı",
            ["kitap"] = "Kitap & Ders Notu",
            ["diger"] = "Diğer"
        };

    private static readonly IReadOnlyDictionary<string, string> CampusAreas =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["yemekhane"] = "Merkez Yemekhane",
            ["kutuphane"] = "Merkez Kütüphane",
            ["kiz_kyk"] = "Kız Öğrenci Yurdu (KYK)",
            ["erkek_kyk"] = "Erkek Öğrenci Yurdu (KYK)",
            ["hastane"] = "Tıp Fakültesi Hastanesi",
            ["muhendislik"] = "Mühendislik Fakültesi",
            ["egitim"] = "Eğitim Fakültesi",
            ["iibf"] = "İİBF",
            ["kafeler"] = "Kampüs Kafeler & Çarşı Alanı",
            ["diger_alan"] = "Diğer Açık Alanlar"
        };

    private static readonly HashSet<string> AllowedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly ILostItemListingService _listingService;
    private readonly IContactSubmissionService _contactSubmissionService;

    public HomeController(
        ApplicationDbContext dbContext,
        IWebHostEnvironment environment,
        ILostItemListingService listingService,
        IContactSubmissionService? contactSubmissionService = null)
    {
        _dbContext = dbContext;
        _environment = environment;
        _listingService = listingService;
        _contactSubmissionService = contactSubmissionService ?? new ContactSubmissionService(dbContext);
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ItemSearchCriteria criteria)
    {
        var result = await _listingService.SearchAsync(criteria);
        ViewBag.Categories = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => category.Name)
            .ToListAsync();

        return View(new ItemSearchViewModel
        {
            Criteria = criteria,
            Items = result.Items,
            ValidationError = result.ValidationError
        });
    }

    public IActionResult Login() => RedirectToAction("Login", "Account");

    public IActionResult Register() => RedirectToAction("Register", "Account");

    [Authorize]
    [HttpGet]
    public IActionResult KayipOlustur() => View("~/Views/Ilan/KayipOlustur.cshtml", new LostItemViewModel());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> KayipOlustur(LostItemViewModel report) =>
        SaveItemAsync(report, isFound: false);

    [Authorize]
    [HttpGet]
    public IActionResult BulunanOlustur() => View("~/Views/Ilan/BulunanOlustur.cshtml", new ItemReportViewModel());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> BulunanOlustur(ItemReportViewModel report) =>
        SaveItemAsync(report, isFound: true);

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profil()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var profile = await _dbContext.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId);
        if (profile is null)
        {
            return NotFound();
        }

        var items = await _dbContext.LostItems
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new UserItemViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Category = item.Category.Name,
                Location = item.Location,
                Date = item.EventDate,
                IsFound = item.IsFound,
                IsItemFound = item.IsItemFound,
                IsDelivered = item.IsDelivered
            })
            .ToListAsync();

        return View("~/Views/Home/Profil.cshtml", new ProfileViewModel
        {
            DisplayName = profile.DisplayName,
            Email = profile.Email,
            LostItems = items.Where(item => !item.IsFound).ToList(),
            FoundItems = items.Where(item => item.IsFound).ToList()
        });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> IlanGuncelle(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var item = await _dbContext.LostItems
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId);

        if (item is null)
        {
            return NotFound();
        }

        ViewBag.Categories = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync();

        return View(new EditItemViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            Location = item.Location,
            EventDate = item.EventDate,
            CategoryId = item.CategoryId
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IlanGuncelle(EditItemViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var item = await _dbContext.LostItems
            .SingleOrDefaultAsync(item => item.Id == model.Id && item.UserId == userId);

        if (item is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _dbContext.Categories
                .AsNoTracking()
                .OrderBy(category => category.Name)
                .ToListAsync();
            return View(model);
        }

        item.Title = model.Title.Trim();
        item.Description = model.Description.Trim();
        item.Location = model.Location.Trim();
        item.EventDate = model.EventDate;
        item.CategoryId = model.CategoryId;
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "İlanınız güncellendi.";
        return RedirectToAction(nameof(Profil));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> IlanSil(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var item = await _dbContext.LostItems
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId);

        if (item is null)
        {
            return NotFound();
        }

        _dbContext.LostItems.Remove(item);
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "İlanınız silindi.";
        return RedirectToAction(nameof(Profil));
    }

    [HttpGet]
    public async Task<IActionResult> Detay(int? ilanId, int? id, int? sourceIlanId = null)
    {
        var item = await _listingService.GetByIdAsync(ilanId ?? id ?? 0);
        if (item is null)
        {
            return View("~/Views/Ilan/Detay.cshtml", new ItemDetailViewModel());
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        string? verificationQuestion = null;
        if (item.IsFound && currentUserId is not null && currentUserId != item.UserId)
        {
            verificationQuestion = await _dbContext.LostItemFeatures
                .AsNoTracking()
                .Where(feature => feature.LostItemId == item.Id && feature.Key == "SecurityQuestion")
                .Select(feature => feature.Value)
                .SingleOrDefaultAsync();
        }
        else if (!item.IsFound && sourceIlanId.HasValue && currentUserId is not null && currentUserId == item.UserId)
        {
            verificationQuestion = await _listingService.GetMatchVerificationQuestionAsync(currentUserId, item.Id, sourceIlanId.Value);
        }

        return View("~/Views/Ilan/Detay.cshtml", new ItemDetailViewModel
        {
            Item = item,
            Recommendations = await _listingService.GetRecommendationsAsync(item),
            CanStartChat = IsOpen(item),
            CurrentUserId = currentUserId,
            VerificationQuestion = verificationQuestion,
            SourceItemId = sourceIlanId,
            SourceItemIsFound = sourceIlanId.HasValue && await _dbContext.LostItems
                .AsNoTracking()
                .Where(candidate => candidate.Id == sourceIlanId.Value)
                .Select(candidate => candidate.IsFound)
                .SingleOrDefaultAsync()
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DogrulaEslesme(VerifyMatchViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["VerificationError"] = "Lütfen doğrulama cevabını yazın.";
            return RedirectToAction(nameof(Detay), new { ilanId = model.LostItemId, sourceIlanId = model.FoundItemId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _listingService.VerifyLostItemMatchAsync(userId, model.LostItemId, model.FoundItemId, model.Answer);
        if (!result.CanStartChat)
        {
            TempData["VerificationError"] = result.ErrorMessage ?? "Doğrulama işlemi tamamlanamadı.";
            return RedirectToAction(nameof(Detay), new { ilanId = model.LostItemId, sourceIlanId = model.FoundItemId });
        }

        return RedirectToAction(nameof(Mesajlar), new
        {
            ilanId = model.FoundItemId,
            eslesenIlanId = model.LostItemId
        });
    }

    private static bool IsOpen(LostItem item) =>
        !item.IsDelivered && (item.IsFound || !item.IsItemFound);

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dogrula(VerifyItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["VerificationError"] = "Lütfen doğrulama cevabını yazın.";
            return RedirectToAction(nameof(Detay), new { ilanId = model.FoundItemId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _listingService.VerifyFoundItemAsync(userId, model.FoundItemId, model.Answer);
        if (!result.CanStartChat)
        {
            TempData["VerificationError"] = result.ErrorMessage ?? "Doğrulama işlemi tamamlanamadı.";
            return RedirectToAction(nameof(Detay), new { ilanId = model.FoundItemId });
        }

        TempData["SuccessMessage"] = result.Succeeded
            ? "Eşya doğrulandı. İlan sahibiyle iletişime geçebilirsiniz."
            : "Cevabınız sohbete kaydedildi. İlan sahibiyle iletişime geçebilirsiniz.";
        return RedirectToAction(nameof(Mesajlar), new
        {
            dogrulananIlanId = model.FoundItemId,
            katilimciId = userId
        });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Mesajlar(
        int? ilanId,
        int? eslesenIlanId,
        int? dogrulananIlanId,
        string? katilimciId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        if (dogrulananIlanId.HasValue)
        {
            var participantUserId = katilimciId ?? userId;
            if (!await _listingService.CanChatWithVerifiedItemAsync(userId, dogrulananIlanId.Value, participantUserId))
            {
                return NotFound();
            }

            var verifiedItem = await _listingService.GetByIdAsync(dogrulananIlanId.Value);
            if (verifiedItem is null)
            {
                return NotFound();
            }

            return View("~/Views/Home/Mesajlar.cshtml", new MessagesViewModel
            {
                Item = verifiedItem,
                IsVerifiedConversation = true,
                VerifiedParticipantUserId = participantUserId
            });
        }

        if (ilanId.HasValue || eslesenIlanId.HasValue)
        {
            if (!ilanId.HasValue ||
                !eslesenIlanId.HasValue ||
                !await _listingService.CanChatAsync(userId, ilanId.Value, eslesenIlanId.Value))
            {
                return NotFound();
            }

            var item = await _listingService.GetByIdAsync(ilanId.Value);
            var matchedItem = await _listingService.GetByIdAsync(eslesenIlanId.Value);
            if (item is null || matchedItem is null)
            {
                return NotFound();
            }

            return View("~/Views/Home/Mesajlar.cshtml", new MessagesViewModel
            {
                Item = item,
                MatchedItem = matchedItem
            });
        }

        return View("~/Views/Home/Mesajlar.cshtml", new MessagesViewModel
        {
            AvailableMatches = await _listingService.GetChatMatchesAsync(userId),
            VerifiedMatches = await _listingService.GetVerifiedChatMatchesAsync(userId)
        });
    }

    public IActionResult KonumPaylas() => View("~/Views/Home/KonumPaylas.cshtml");

    public IActionResult Hakkimizda() => View("~/Views/Home/Hakkimizda.cshtml");

    [HttpGet]
    public IActionResult Iletisim() => View("~/Views/Home/Iletisim.cshtml", new ContactViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Iletisim(ContactViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Home/Iletisim.cshtml", model);
        }

        await _contactSubmissionService.CreateAsync(
            new ContactSubmissionRequest(
                model.Name,
                model.Email,
                model.Subject,
                model.Message,
                User.FindFirstValue(ClaimTypes.NameIdentifier)),
            cancellationToken);

        TempData["ContactSuccessMessage"] = "Mesajınız başarıyla gönderildi. En kısa sürede size dönüş yapacağız.";
        return RedirectToAction(nameof(Iletisim));
    }

    public IActionResult Privacy() => View();

    private async Task<IActionResult> SaveItemAsync(ItemReportBaseViewModel model, bool isFound)
    {
        if (!CategoryNames.ContainsKey(model.Category ?? string.Empty))
        {
            ModelState.AddModelError(nameof(model.Category), "Lütfen geçerli bir kategori seçin.");
        }

        if (!CampusAreas.ContainsKey(model.CampusArea ?? string.Empty))
        {
            ModelState.AddModelError(nameof(model.CampusArea), "Lütfen geçerli bir kampüs bölgesi seçin.");
        }

        if (isFound && model is ItemReportViewModel foundReport && string.IsNullOrWhiteSpace(foundReport.SecurityQuestion))
        {
            ModelState.AddModelError(nameof(ItemReportViewModel.SecurityQuestion), "Doğrulama sorusu zorunludur.");
        }

        if (!isFound && string.IsNullOrWhiteSpace(model.Description))
        {
            ModelState.AddModelError(nameof(model.Description), "Eşyanın açıklaması zorunludur.");
        }

        if (isFound && model.ItemStatus is not ("with_me" or "delivered"))
        {
            ModelState.AddModelError(nameof(model.ItemStatus), "Lütfen eşyanın mevcut durumunu seçin.");
        }

        if (model.Photo is { Length: > 0 } &&
            (model.Photo.Length > 5 * 1024 * 1024 ||
             !AllowedImageExtensions.Contains(Path.GetExtension(model.Photo.FileName))))
        {
            ModelState.AddModelError(nameof(model.Photo), "En fazla 5 MB boyutunda JPG, PNG, GIF veya WebP fotoğraf yükleyin.");
        }

        if (!ModelState.IsValid)
        {
            return View(isFound ? "~/Views/Ilan/BulunanOlustur.cshtml" : "~/Views/Ilan/KayipOlustur.cshtml", model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var profileExists = await _dbContext.UserProfiles.AnyAsync(profile => profile.Id == userId);
        if (!profileExists)
        {
            ModelState.AddModelError(string.Empty, "Hesap profiliniz bulunamadı. Lütfen yeniden giriş yapın.");
            return View(isFound ? "~/Views/Ilan/BulunanOlustur.cshtml" : "~/Views/Ilan/KayipOlustur.cshtml", model);
        }

        var categoryName = CategoryNames[model.Category!];
        var category = await _dbContext.Categories
            .OrderBy(item => item.Id)
            .FirstOrDefaultAsync(item => item.Name == categoryName);
        if (category is null)
        {
            category = new Category { Name = categoryName };
            _dbContext.Categories.Add(category);
        }

        string? imagePath = null;
        if (model.Photo is { Length: > 0 })
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var uploadDirectory = Path.Combine(webRoot, "uploads", "items");
            Directory.CreateDirectory(uploadDirectory);

            var extension = Path.GetExtension(model.Photo.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadDirectory, fileName);
            imagePath = $"/uploads/items/{fileName}";

            await using var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
            await model.Photo.CopyToAsync(fileStream);
        }

        var campusArea = CampusAreas[model.CampusArea!];
        var location = string.IsNullOrWhiteSpace(model.LocationDetail)
            ? campusArea
            : $"{campusArea} - {model.LocationDetail.Trim()}";

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var lostItem = new LostItem
            {
                Title = model.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description)
                    ? "Buluntu eşya bildirimi."
                    : model.Description.Trim(),
                ImagePath = imagePath,
                Location = location,
                CreatedAt = DateTime.UtcNow,
                EventDate = model.ItemDate!.Value,
                IsFound = isFound,
                IsItemFound = isFound,
                IsDelivered = isFound && model.ItemStatus == "delivered",
                UserId = userId,
                Category = category
            };
            _dbContext.LostItems.Add(lostItem);

            await _dbContext.SaveChangesAsync();

            var features = new List<LostItemFeature>();

            if (isFound && model is ItemReportViewModel foundModel)
            {
                features.Add(new LostItemFeature
                {
                    LostItemId = lostItem.Id,
                    Key = "SecurityQuestion",
                    Value = foundModel.SecurityQuestion.Trim(),
                    IsHidden = true
                });

                features.Add(new LostItemFeature
                {
                    LostItemId = lostItem.Id,
                    Key = "SecurityAnswerHash",
                    Value = new PasswordHasher<LostItem>().HashPassword(lostItem, foundModel.SecurityAnswer.Trim()),
                    IsHidden = true
                });
            }

            AddFeature(features, lostItem.Id, "Brand", model.Brand, isFound);
            AddFeature(features, lostItem.Id, "Model", model.Model, isFound);
            AddFeature(features, lostItem.Id, "Color", model.Color, isFound);

            if (isFound)
            {
                features.Add(new LostItemFeature
                {
                    LostItemId = lostItem.Id,
                    Key = "ItemStatus",
                    Value = model.ItemStatus!,
                    IsHidden = true
                });

                if (model.ItemStatus == "delivered" && !string.IsNullOrWhiteSpace(model.DeliveredTo))
                {
                    features.Add(new LostItemFeature
                    {
                        LostItemId = lostItem.Id,
                        Key = "DeliveredTo",
                        Value = model.DeliveredTo.Trim(),
                        IsHidden = true
                    });
                }
            }

            _dbContext.LostItemFeatures.AddRange(features);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            if (imagePath is not null)
            {
                var fullPath = Path.Combine(
                    _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
                    imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            throw;
        }

        TempData["SuccessMessage"] = isFound
            ? "Bulunan eşya bildirildi ve kaydedildi."
            : "Kayıp ilanınız yayınlandı ve kaydedildi.";
        return RedirectToAction(nameof(Profil));
    }

    private static void AddFeature(
        List<LostItemFeature> features,
        int itemId,
        string key,
        string? value,
        bool isHidden)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            features.Add(new LostItemFeature
            {
                LostItemId = itemId,
                Key = key,
                Value = value.Trim(),
                IsHidden = isHidden
            });
        }
    }
}

public abstract class ItemReportBaseViewModel
{
    [Required(ErrorMessage = "Eşya başlığı zorunludur.")]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori seçin.")]
    public string? Category { get; set; }

    [Required(ErrorMessage = "Tarih ve saat zorunludur.")]
    public DateTime? ItemDate { get; set; }

    [Required(ErrorMessage = "Kampüs bölgesi seçin.")]
    public string CampusArea { get; set; } = string.Empty;

    [StringLength(300)]
    public string? LocationDetail { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? Brand { get; set; }

    [StringLength(100)]
    public string? Model { get; set; }

    [StringLength(100)]
    public string? Color { get; set; }

    public string? ItemStatus { get; set; } = "with_me";

    [StringLength(300)]
    public string? DeliveredTo { get; set; }

    [FromForm(Name = "itemPhoto")]
    public IFormFile? Photo { get; set; }
}

public sealed class LostItemViewModel : ItemReportBaseViewModel
{
}

public sealed class ItemReportViewModel : ItemReportBaseViewModel
{
    [Required(ErrorMessage = "Doğrulama sorusu zorunludur.")]
    [StringLength(300)]
    public string SecurityQuestion { get; set; } = string.Empty;

    [Required(ErrorMessage = "Doğrulama cevabı zorunludur.")]
    [StringLength(300)]
    public string SecurityAnswer { get; set; } = string.Empty;
}

public sealed class ContactViewModel
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konu seçin.")]
    [StringLength(100)]
    public string Subject { get; set; } = "Destek";

    [Required(ErrorMessage = "Mesajınızı yazın.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Mesajınız 10-2000 karakter arasında olmalıdır.")]
    public string Message { get; set; } = string.Empty;
}

public class ProfileViewModel
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<UserItemViewModel> LostItems { get; set; } = [];
    public List<UserItemViewModel> FoundItems { get; set; } = [];
}

public class UserItemViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsFound { get; set; }
    public bool IsItemFound { get; set; }
    public bool IsDelivered { get; set; }
}

public class ItemSearchViewModel
{
    public ItemSearchCriteria Criteria { get; set; } = new();
    public IReadOnlyList<LostItem> Items { get; set; } = [];
    public IReadOnlyList<ChatMatch> Matches { get; set; } = [];
    public string? ValidationError { get; set; }
}

public class ItemDetailViewModel
{
    public LostItem? Item { get; set; }
    public IReadOnlyList<RecommendedItem> Recommendations { get; set; } = [];
    public bool CanStartChat { get; set; }
    public string? CurrentUserId { get; set; }
    public string? VerificationQuestion { get; set; }
    public int? SourceItemId { get; set; }
    public bool SourceItemIsFound { get; set; }
}

public sealed class VerifyItemViewModel
{
    [Range(1, int.MaxValue)]
    public int FoundItemId { get; set; }

    [Required(ErrorMessage = "Doğrulama cevabı zorunludur.")]
    [StringLength(300)]
    public string Answer { get; set; } = string.Empty;
}

public sealed class VerifyMatchViewModel
{
    [Range(1, int.MaxValue)]
    public int LostItemId { get; set; }

    [Range(1, int.MaxValue)]
    public int FoundItemId { get; set; }

    [Required(ErrorMessage = "Doğrulama cevabı zorunludur.")]
    [StringLength(300)]
    public string Answer { get; set; } = string.Empty;
}

public class MessagesViewModel
{
    public LostItem? Item { get; set; }
    public LostItem? MatchedItem { get; set; }
    public IReadOnlyList<ChatMatch> AvailableMatches { get; set; } = [];
    public IReadOnlyList<VerifiedChatMatch> VerifiedMatches { get; set; } = [];
    public bool IsVerifiedConversation { get; set; }
    public string? VerifiedParticipantUserId { get; set; }
}

public class EditItemViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "İlan başlığı zorunludur.")]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konum zorunludur.")]
    [StringLength(250)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tarih zorunludur.")]
    public DateTime EventDate { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Kategori seçiniz.")]
    public int CategoryId { get; set; }
}
