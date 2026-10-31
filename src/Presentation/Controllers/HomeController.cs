using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ARABULUCU.BLL.DTOs;
using ARABULUCU.BLL.Services;
using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
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

    public HomeController(
        ApplicationDbContext dbContext,
        IWebHostEnvironment environment,
        ILostItemListingService listingService)
    {
        _dbContext = dbContext;
        _environment = environment;
        _listingService = listingService;
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
    public IActionResult KayipOlustur() => View("~/Views/Ilan/KayipOlustur.cshtml", new ItemReportViewModel());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> KayipOlustur(ItemReportViewModel report) =>
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

    [HttpGet]
    public async Task<IActionResult> Detay(int? ilanId, int? id)
    {
        var item = await _listingService.GetByIdAsync(ilanId ?? id ?? 0);
        if (item is null)
        {
            return View("~/Views/Ilan/Detay.cshtml", new ItemDetailViewModel());
        }

        return View("~/Views/Ilan/Detay.cshtml", new ItemDetailViewModel
        {
            Item = item,
            Recommendations = await _listingService.GetRecommendationsAsync(item)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Mesajlar(int? ilanId)
    {
        if (ilanId.HasValue)
        {
            var item = await _listingService.GetByIdAsync(ilanId.Value);
            if (item is null)
            {
                return NotFound();
            }

            return View("~/Views/Home/Mesajlar.cshtml", new MessagesViewModel
            {
                Item = item
            });
        }

        var result = await _listingService.SearchAsync(new ItemSearchCriteria());
        return View("~/Views/Home/Mesajlar.cshtml", new MessagesViewModel
        {
            AvailableItems = result.Items
        });
    }

    public IActionResult KonumPaylas() => View("~/Views/Home/KonumPaylas.cshtml");

    public IActionResult Hakkimizda() => View("~/Views/Home/Hakkimizda.cshtml");

    public IActionResult Iletisim() => View("~/Views/Home/Iletisim.cshtml");

    public IActionResult Privacy() => View();

    private async Task<IActionResult> SaveItemAsync(ItemReportViewModel model, bool isFound)
    {
        if (!CategoryNames.ContainsKey(model.Category ?? string.Empty))
        {
            ModelState.AddModelError(nameof(model.Category), "Lütfen geçerli bir kategori seçin.");
        }

        if (!CampusAreas.ContainsKey(model.CampusArea ?? string.Empty))
        {
            ModelState.AddModelError(nameof(model.CampusArea), "Lütfen geçerli bir kampüs bölgesi seçin.");
        }

        if (string.IsNullOrWhiteSpace(model.SecurityQuestion))
        {
            ModelState.AddModelError(nameof(model.SecurityQuestion), "Doğrulama sorusu zorunludur.");
        }

        if (!isFound && string.IsNullOrWhiteSpace(model.Description))
        {
            ModelState.AddModelError(nameof(model.Description), "Eşyanın açıklaması zorunludur.");
        }

        if (!isFound && string.IsNullOrWhiteSpace(model.SecurityAnswer))
        {
            ModelState.AddModelError(nameof(model.SecurityAnswer), "Doğrulama cevabı zorunludur.");
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

            var features = new List<LostItemFeature>
            {
                new()
                {
                    LostItemId = lostItem.Id,
                    Key = "SecurityQuestion",
                    Value = model.SecurityQuestion!.Trim(),
                    IsHidden = true
                }
            };

            AddFeature(features, lostItem.Id, "Brand", model.Brand, isFound);
            AddFeature(features, lostItem.Id, "Model", model.Model, isFound);
            AddFeature(features, lostItem.Id, "Color", model.Color, isFound);

            if (!isFound)
            {
                features.Add(new LostItemFeature
                {
                    LostItemId = lostItem.Id,
                    Key = "SecurityAnswerHash",
                    Value = new PasswordHasher<LostItem>().HashPassword(lostItem, model.SecurityAnswer!.Trim()),
                    IsHidden = true
                });
            }
            else
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

public class ItemReportViewModel
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

    [StringLength(300)]
    public string? SecurityQuestion { get; set; }

    [StringLength(300)]
    public string? SecurityAnswer { get; set; }

    public string? ItemStatus { get; set; } = "with_me";

    [StringLength(300)]
    public string? DeliveredTo { get; set; }

    [FromForm(Name = "itemPhoto")]
    public IFormFile? Photo { get; set; }
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
    public string? ValidationError { get; set; }
}

public class ItemDetailViewModel
{
    public LostItem? Item { get; set; }
    public IReadOnlyList<RecommendedItem> Recommendations { get; set; } = [];
}

public class MessagesViewModel
{
    public LostItem? Item { get; set; }
    public IReadOnlyList<LostItem> AvailableItems { get; set; } = [];
}
