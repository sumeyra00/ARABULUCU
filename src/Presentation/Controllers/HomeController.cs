using System.Security.Claims;
using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;
using ARABULUCU.Web.Models.Listing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ARABULUCU.Web.Controllers
{
    public class HomeController : Controller
    {
        private static readonly HashSet<string> CampusAreas = new(StringComparer.Ordinal)
        {
            "yemekhane", "kutuphane", "kiz_kyk", "erkek_kyk", "hastane",
            "muhendislik", "egitim", "iibf", "kafeler", "diger_alan"
        };

        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };

        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _environment;

        public HomeController(ApplicationDbContext dbContext, IWebHostEnvironment environment)
        {
            _dbContext = dbContext;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }
        [Authorize]
        public IActionResult KayipOlustur()
        {
            return View("~/Views/Ilan/KayipOlustur.cshtml");
        }
        [Authorize]
        public IActionResult BulunanOlustur()
        {
            return View("~/Views/Ilan/BulunanOlustur.cshtml", new FoundItemViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(5_500_000)]
        public async Task<IActionResult> BulunanOlustur(FoundItemViewModel model)
        {
            if (model.FoundDate == default)
            {
                ModelState.AddModelError(nameof(model.FoundDate), "Geçerli bir tarih ve saat girin.");
            }

            if (!CampusAreas.Contains(model.CampusArea))
            {
                ModelState.AddModelError(nameof(model.CampusArea), "Geçerli bir kampüs bölgesi seçin.");
            }

            if (model.ItemStatus is not ("with_me" or "delivered"))
            {
                ModelState.AddModelError(nameof(model.ItemStatus), "Eşyanın mevcut durumunu seçin.");
            }

            if (model.ItemStatus == "delivered" && string.IsNullOrWhiteSpace(model.DeliveredTo))
            {
                ModelState.AddModelError(nameof(model.DeliveredTo), "Teslim ettiğiniz yeri veya birimi yazın.");
            }

            var imageExtension = model.ItemPhoto is null
                ? null
                : Path.GetExtension(model.ItemPhoto.FileName);
            if (model.ItemPhoto is not null
                && (model.ItemPhoto.Length == 0
                    || model.ItemPhoto.Length > 5 * 1024 * 1024
                    || imageExtension is null
                    || !AllowedImageExtensions.Contains(imageExtension)))
            {
                ModelState.AddModelError(nameof(model.ItemPhoto), "Fotoğraf 5 MB'dan küçük JPG, PNG, GIF veya WebP olmalıdır.");
            }

            if (!ModelState.IsValid)
            {
                return View("~/Views/Ilan/BulunanOlustur.cshtml", model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var profile = await _dbContext.UserProfiles.FindAsync(userId);
            if (profile is null)
            {
                profile = new UserProfile
                {
                    Id = userId,
                    DisplayName = User.Identity?.Name ?? "Kullanıcı",
                    Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty
                };
                _dbContext.UserProfiles.Add(profile);
            }

            string? imagePath = null;
            string? savedImagePath = null;
            var location = string.IsNullOrWhiteSpace(model.LocationDetail)
                ? model.CampusArea
                : $"{model.CampusArea} - {model.LocationDetail.Trim()}";
            var status = model.ItemStatus == "delivered"
                ? $"Yetkili birime teslim edildi: {model.DeliveredTo?.Trim()}"
                : "Eşya ilanı veren kişide.";
            var item = new LostItem
            {
                Title = model.Title.Trim(),
                Description = $"Bulunma tarihi: {model.FoundDate:dd.MM.yyyy HH:mm}. {status}",
                ImagePath = imagePath,
                Location = location,
                CreatedAt = DateTime.UtcNow,
                IsFound = true,
                UserId = userId,
                CategoryId = model.CategoryId
            };

            try
            {
                if (model.ItemPhoto is not null && imageExtension is not null)
                {
                    var webRootPath = _environment.WebRootPath
                        ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
                    var uploadsDirectory = Path.Combine(webRootPath, "uploads", "items");
                    Directory.CreateDirectory(uploadsDirectory);

                    var fileName = $"{Guid.NewGuid():N}{imageExtension.ToLowerInvariant()}";
                    savedImagePath = Path.Combine(uploadsDirectory, fileName);
                    await using (var stream = new FileStream(savedImagePath, FileMode.CreateNew))
                    {
                        await model.ItemPhoto.CopyToAsync(stream);
                    }

                    imagePath = $"/uploads/items/{fileName}";
                    item.ImagePath = imagePath;
                }

                await using var transaction = await _dbContext.Database.BeginTransactionAsync();
                _dbContext.LostItems.Add(item);
                await _dbContext.SaveChangesAsync();

                _dbContext.LostItemFeatures.Add(new LostItemFeature
                {
                    LostItemId = item.Id,
                    Key = "Doğrulama sorusu",
                    Value = model.SecurityQuestion.Trim(),
                    IsHidden = true
                });
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                if (savedImagePath is not null && System.IO.File.Exists(savedImagePath))
                {
                    System.IO.File.Delete(savedImagePath);
                }

                throw;
            }

            TempData["FoundItemSuccess"] = $"Buluntu eşya ilanınız kaydedildi. İlan numarası: {item.Id}";
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Detay()
        {
            return View("~/Views/Ilan/Detay.cshtml");
        }
        public IActionResult Mesajlar()
        {
            return View("~/Views/Home/Mesajlar.cshtml");
        }
        public IActionResult Profil()
        {
            return View("~/Views/Home/Profil.cshtml");
        }
        public IActionResult KonumPaylas()
        {
            return View("~/Views/Home/KonumPaylas.cshtml");
        }
        public IActionResult Privacy()
        {
            return View();
        }
    }
}