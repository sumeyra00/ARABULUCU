using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
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
        return View(await BuildHomeViewModelAsync(criteria, result.Items, result.ValidationError));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Match(MatchingInput input)
    {
        if (!ModelState.IsValid)
        {
            var invalidSearch = await _listingService.SearchAsync(new ItemSearchCriteria());
            return View(nameof(Index), await BuildHomeViewModelAsync(
                new ItemSearchCriteria(), invalidSearch.Items, "Konum ve tarih alanları zorunludur.", true));
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _listingService.FindMatchesAsync(new MatchingCriteria
        {
            Category = CategoryNames.TryGetValue(input.Category ?? string.Empty, out var categoryName)
                ? categoryName
                : input.Category ?? string.Empty,
            Title = input.Title ?? string.Empty,
            Location = input.Location,
            Brand = input.Brand,
            Model = input.Model,
            Color = input.Color,
            EventDate = input.EventDate!.Value.Date + input.EventTime!.Value,
            SourceIsFound = input.SourceIsFound,
            CurrentUserId = currentUserId
        });

        return View(nameof(Index), await BuildHomeViewModelAsync(
            new ItemSearchCriteria
            {
                Category = input.Category,
                Query = input.Title,
                Location = input.Location,
                Brand = input.Brand,
                Model = input.Model,
                Color = input.Color,
                EventDateFrom = input.EventDate.Value.Date + input.EventTime.Value,
                EventDateTo = input.EventDate.Value.Date + input.EventTime.Value
            },
            result.Items,
            result.Error,
            true));
    }

    private async Task<ItemSearchViewModel> BuildHomeViewModelAsync(
        ItemSearchCriteria criteria,
        IReadOnlyList<ARABULUCU.ENT.Entities.LostItem> items,
        string? validationError = null,
        bool isMatchingResult = false)
    {
        var questions = new Dictionary<int, IReadOnlyList<VerificationQuestionDto>>();
        foreach (var item in items)
        {
            questions[item.Id] = await _listingService.GetVerificationQuestionsAsync(
                item.Id,
                includeSecretQuestion: false);
        }

        return new ItemSearchViewModel
        {
            Criteria = criteria,
            Items = items,
            CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            ValidationError = validationError ?? TempData["VerificationError"]?.ToString(),
            VerificationQuestions = questions,
            IsMatchingResult = isMatchingResult
        };
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(
        int ilanId,
        List<VerificationAnswerInput> answers,
        string? returnUrl = null,
        string? successUrl = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _listingService.VerifyAsync(
            ilanId,
            userId,
            answers.Select(answer => new VerificationAnswerDto
            {
                QuestionId = answer.QuestionId,
                Answer = answer.Answer ?? string.Empty
            }).ToArray());

        if (!result.IsSuccessful)
        {
            TempData["VerificationError"] = result.Error ?? "Doğrulama başarısız.";
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(Index));
        }

        return !string.IsNullOrWhiteSpace(successUrl) && Url.IsLocalUrl(successUrl)
            ? Redirect(successUrl)
            : RedirectToAction(nameof(Detay), new { ilanId });
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
    public async Task<IActionResult> Duzenle(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var item = await _dbContext.LostItems
            .Include(listing => listing.Category)
            .Include(listing => listing.Features)
            .SingleOrDefaultAsync(listing => listing.Id == id && listing.UserId == userId);
        if (item is null)
        {
            return NotFound();
        }

        var locationParts = item.Location.Split(" - ", 2, StringSplitOptions.None);
        var campusArea = CampusAreas.FirstOrDefault(area => area.Value == locationParts[0]).Key;
        var status = item.Features.FirstOrDefault(feature => feature.Key == "ItemStatus")?.Value;
        return View(item.IsFound ? "~/Views/Ilan/BulunanOlustur.cshtml" : "~/Views/Ilan/KayipOlustur.cshtml", new ItemReportViewModel
        {
            Id = item.Id,
            IsEditMode = true,
            Title = item.Title,
            Category = CategoryNames.FirstOrDefault(category => category.Value == item.Category.Name).Key,
            ItemDate = item.EventDate,
            CampusArea = campusArea,
            LocationDetail = locationParts.Length > 1 ? locationParts[1] : null,
            Description = item.Description,
            Brand = item.Features.FirstOrDefault(feature => feature.Key == "Brand")?.Value,
            Model = item.Features.FirstOrDefault(feature => feature.Key == "Model")?.Value,
            Color = item.Features.FirstOrDefault(feature => feature.Key == "Color")?.Value,
            SecurityQuestion = item.Features.FirstOrDefault(feature => feature.IsVerificationQuestion)?.Value,
            ItemStatus = status ?? (item.IsDelivered ? "delivered" : "with_me"),
            DeliveredTo = item.Features.FirstOrDefault(feature => feature.Key == "DeliveredTo")?.Value
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(ItemReportViewModel report)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var item = await _dbContext.LostItems
            .Include(listing => listing.Features)
            .SingleOrDefaultAsync(listing => listing.Id == report.Id && listing.UserId == userId);
        if (item is null)
        {
            return NotFound();
        }

        if (!CategoryNames.ContainsKey(report.Category ?? string.Empty) ||
            !CampusAreas.ContainsKey(report.CampusArea) ||
            (!item.IsFound && string.IsNullOrWhiteSpace(report.Description)) ||
            string.IsNullOrWhiteSpace(report.SecurityAnswer) ||
            string.IsNullOrWhiteSpace(report.SecurityQuestion) ||
            (item.IsFound && report.ItemStatus is not ("with_me" or "delivered")))
        {
            ModelState.AddModelError(string.Empty, "Lütfen tüm zorunlu alanları geçerli şekilde doldurun.");
        }

        if (!ModelState.IsValid)
        {
            report.IsEditMode = true;
            return View(item.IsFound ? "~/Views/Ilan/BulunanOlustur.cshtml" : "~/Views/Ilan/KayipOlustur.cshtml", report);
        }

        var category = await _dbContext.Categories.FirstOrDefaultAsync(existing => existing.Name == CategoryNames[report.Category!]);
        if (category is null)
        {
            category = new Category { Name = CategoryNames[report.Category!] };
            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync();
        }
        var location = string.IsNullOrWhiteSpace(report.LocationDetail)
            ? CampusAreas[report.CampusArea]
            : $"{CampusAreas[report.CampusArea]} - {report.LocationDetail.Trim()}";
        item.Title = report.Title.Trim();
        item.Description = string.IsNullOrWhiteSpace(report.Description) ? "Buluntu eşya bildirimi." : report.Description.Trim();
        item.Location = location;
        item.EventDate = report.ItemDate!.Value;
        item.Category = category;
        item.CategoryId = category.Id;
        item.IsDelivered = item.IsFound && report.ItemStatus == "delivered";

        if (report.Photo is { Length: > 0 })
        {
            item.ImagePath = await SaveImageAsync(report.Photo);
        }

        _dbContext.LostItemFeatures.RemoveRange(item.Features);
        var features = new List<LostItemFeature>();
        AddVerificationFeature(features, item.Id, "SecurityQuestion", report.SecurityQuestion, report.SecurityAnswer, 30);
        AddVerificationFeature(features, item.Id, "Location", location, location, 20);
        AddVerificationFeature(
            features,
            item.Id,
            "EventDate",
            item.EventDate.ToString("dd.MM.yyyy HH:mm"),
            item.EventDate.ToString("dd.MM.yyyy HH:mm"),
            20);
        AddVerificationFeature(features, item.Id, "Brand", report.Brand, report.Brand, 20);
        AddVerificationFeature(features, item.Id, "Model", report.Model, report.Model, 20);
        AddVerificationFeature(features, item.Id, "Color", report.Color, report.Color, 20);
        if (item.IsFound)
        {
            features.Add(new LostItemFeature { LostItemId = item.Id, Key = "ItemStatus", Value = report.ItemStatus!, IsHidden = true });
            AddFeature(features, item.Id, "DeliveredTo", report.DeliveredTo, true);
        }

        _dbContext.LostItemFeatures.AddRange(features);
        _dbContext.ListingStatusHistories.Add(new ListingStatusHistory
        {
            LostItemId = item.Id,
            Status = "Created",
            ChangedByUserId = userId,
            Details = "İlan oluşturuldu."
        });
        await _dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "İlanınız güncellendi.";
        return RedirectToAction(nameof(Profil));
    }

    private async Task<string> SaveImageAsync(IFormFile photo)
    {
        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadDirectory = Path.Combine(webRoot, "uploads", "items");
        Directory.CreateDirectory(uploadDirectory);
        var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadDirectory, fileName);
        await using var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
        await photo.CopyToAsync(fileStream);
        return $"/uploads/items/{fileName}";
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var item = await _dbContext.LostItems.SingleOrDefaultAsync(listing => listing.Id == id && listing.UserId == userId);
        if (item is null)
        {
            return NotFound();
        }

        _dbContext.LostItems.Remove(item);
        await _dbContext.SaveChangesAsync();
        TempData["SuccessMessage"] = "İlanınız silindi.";
        return RedirectToAction(nameof(Profil));
    }

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

    [HttpGet]
    public async Task<IActionResult> Detay(int? ilanId, int? id)
    {
        var item = await _listingService.GetByIdAsync(ilanId ?? id ?? 0);
        if (item is null)
        {
            return View("~/Views/Ilan/Detay.cshtml", new ItemDetailViewModel());
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var recommendations = await _listingService.GetRecommendationsAsync(item);
        var verificationQuestions = new Dictionary<int, IReadOnlyList<VerificationQuestionDto>>();
        var verifiedRecommendationIds = new HashSet<int>();
        ChatMatch? contactMatch = null;

        if (currentUserId is not null &&
            currentUserId != item.UserId &&
            IsOpen(item))
        {
            contactMatch = (await _listingService.GetChatMatchesAsync(currentUserId))
                .Where(match => match.MatchedItem.Id == item.Id)
                .OrderByDescending(match => match.SimilarityPercent)
                .FirstOrDefault();

            if (contactMatch is not null)
            {
                verificationQuestions[item.Id] =
                    await _listingService.GetVerificationQuestionsAsync(
                        item.Id,
                        includeSecretQuestion: true);

                if (await _listingService.GetProtectedDetailsAsync(item.Id, currentUserId) is not null)
                {
                    verifiedRecommendationIds.Add(item.Id);
                }
            }
        }

        foreach (var recommendation in recommendations)
        {
            var verificationItemId = recommendation.Item.UserId == currentUserId
                ? item.Id
                : recommendation.Item.Id;
            verificationQuestions[verificationItemId] =
                await _listingService.GetVerificationQuestionsAsync(
                    verificationItemId,
                    includeSecretQuestion: true);

            if (currentUserId is not null &&
                await _listingService.GetProtectedDetailsAsync(verificationItemId, currentUserId) is not null)
            {
                verifiedRecommendationIds.Add(verificationItemId);
            }
        }

        return View("~/Views/Ilan/Detay.cshtml", new ItemDetailViewModel
        {
            Item = item,
            Recommendations = recommendations,
            CanStartChat = IsOpen(item),
            CurrentUserId = currentUserId,
            ContactMatch = contactMatch,
            VerifiedRecommendationIds = verifiedRecommendationIds,
            VerificationQuestions = verificationQuestions,
            StatusHistory = await _dbContext.ListingStatusHistories
                .AsNoTracking()
                .Where(history => history.LostItemId == item.Id)
                .OrderByDescending(history => history.CreatedAt)
                .ToListAsync(),
            ProtectedDetails = currentUserId is { } userId
                ? await _listingService.GetProtectedDetailsAsync(item.Id, userId)
                : null
        });
    }

    private static bool IsOpen(LostItem item) =>
        !item.IsArchived && !item.IsDelivered && (item.IsFound || !item.IsItemFound);

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Mesajlar(int? ilanId, int? eslesenIlanId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
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

            if (!await _dbContext.ListingStatusHistories.AnyAsync(history =>
                    history.LostItemId == ilanId.Value && history.Status == "ChatStarted" &&
                    history.ChangedByUserId == userId && history.Details!.Contains(matchedItem.Id.ToString())))
            {
                item.LastActivityAt = DateTime.UtcNow;
                _dbContext.ListingStatusHistories.Add(new ListingStatusHistory
                {
                    LostItemId = item.Id,
                    Status = "ChatStarted",
                    ChangedByUserId = userId,
                    Details = $"Eşleşen ilan #{matchedItem.Id} ile sohbet başlatıldı."
                });
                await _dbContext.SaveChangesAsync();
            }

            return View("~/Views/Home/Mesajlar.cshtml", new MessagesViewModel
            {
                Item = item,
                MatchedItem = matchedItem
            });
        }

        return View("~/Views/Home/Mesajlar.cshtml", new MessagesViewModel
        {
            AvailableMatches = await _listingService.GetChatMatchesAsync(userId)
        });
    }

    public IActionResult KonumPaylas() => View("~/Views/Home/KonumPaylas.cshtml");

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportListing(ListingReportInput input)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var exists = await _dbContext.LostItems.AnyAsync(item => item.Id == input.LostItemId);
        if (!exists || string.IsNullOrWhiteSpace(input.Reason))
        {
            return BadRequest();
        }

        var alreadyReported = await _dbContext.ListingReports.AnyAsync(report =>
            report.LostItemId == input.LostItemId && report.ReporterUserId == userId && report.Status == "Pending");
        if (!alreadyReported)
        {
            _dbContext.ListingReports.Add(new ListingReport
            {
                LostItemId = input.LostItemId,
                ReporterUserId = userId,
                Reason = input.Reason.Trim(),
                Details = input.Details?.Trim()
            });
            await _dbContext.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "İlan şikâyetiniz moderasyon kuyruğuna gönderildi.";
        return RedirectToAction(nameof(Detay), new { ilanId = input.LostItemId });
    }

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

        if (!isFound && string.IsNullOrWhiteSpace(model.Description))
        {
            ModelState.AddModelError(nameof(model.Description), "Eşyanın açıklaması zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(model.SecurityAnswer))
        {
            ModelState.AddModelError(nameof(model.SecurityAnswer), "Gizli doğrulama detayı zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(model.SecurityQuestion))
        {
            ModelState.AddModelError(nameof(model.SecurityQuestion), "Gizli doğrulama sorusu zorunludur.");
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
                LastActivityAt = DateTime.UtcNow,
                UserId = userId,
                Category = category
            };
            _dbContext.LostItems.Add(lostItem);

            await _dbContext.SaveChangesAsync();

            var features = new List<LostItemFeature>();
            AddVerificationFeature(
                features,
                lostItem.Id,
                // Key geriye dönük olarak korunur; formdaki SecurityAnswer
                // yalnızca gizli cevaptır ve hash'lenerek saklanır.
                "SecurityQuestion",
                model.SecurityQuestion,
                model.SecurityAnswer,
                30);
            AddVerificationFeature(features, lostItem.Id, "Location", location, location, 20);
            AddVerificationFeature(
                features,
                lostItem.Id,
                "EventDate",
                lostItem.EventDate.ToString("dd.MM.yyyy HH:mm"),
                lostItem.EventDate.ToString("dd.MM.yyyy HH:mm"),
                20);
            AddVerificationFeature(features, lostItem.Id, "Brand", model.Brand, model.Brand, 20);
            AddVerificationFeature(features, lostItem.Id, "Model", model.Model, model.Model, 20);
            AddVerificationFeature(features, lostItem.Id, "Color", model.Color, model.Color, 20);

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
            _dbContext.ListingStatusHistories.Add(new ListingStatusHistory
            {
                LostItemId = lostItem.Id,
                Status = "Created",
                ChangedByUserId = userId,
                Details = "İlan oluşturuldu."
            });
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

    private static void AddVerificationFeature(
        List<LostItemFeature> features,
        int itemId,
        string prompt,
        string? value,
        string? answer,
        int weight)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(answer))
        {
            return;
        }

        var feature = new LostItemFeature
        {
            LostItemId = itemId,
            Key = prompt,
            Value = value.Trim(),
            IsHidden = true,
            IsVerificationQuestion = true,
            Weight = weight
        };
        feature.AnswerHash = new PasswordHasher<LostItemFeature>()
            .HashPassword(feature, NormalizeAnswer(answer));
        features.Add(feature);
    }

    private static string NormalizeAnswer(string value) =>
        string.Join(' ', value.Trim().Normalize(NormalizationForm.FormKC)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
}

public class MatchingInput
{
    [Required(ErrorMessage = "İlan türünü seçin.")]
    public bool? SourceIsFound { get; set; }

    [Required(ErrorMessage = "Kategori seçin.")]
    public string? Category { get; set; }

    [Required(ErrorMessage = "Eşya başlığı zorunludur.")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Konum zorunludur.")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tarih zorunludur.")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Saat zorunludur.")]
    public TimeSpan? EventTime { get; set; }

    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
}

public class ItemReportViewModel
{
    public int Id { get; set; }
    public bool IsEditMode { get; set; }

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

    [Required(ErrorMessage = "Gizli doğrulama detayı zorunludur.")]
    [StringLength(300)]
    public string? SecurityAnswer { get; set; }

    [Required(ErrorMessage = "Gizli doğrulama sorusu zorunludur.")]
    [StringLength(300)]
    public string? SecurityQuestion { get; set; }

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
    public string? CurrentUserId { get; set; }
    public string? ValidationError { get; set; }
    public IReadOnlyDictionary<int, IReadOnlyList<VerificationQuestionDto>> VerificationQuestions { get; set; } =
        new Dictionary<int, IReadOnlyList<VerificationQuestionDto>>();
    public IReadOnlyList<VerificationQuestionDto> MatchingQuestions { get; set; } = [];
    public bool IsMatchingResult { get; set; }
}

public class VerificationAnswerInput
{
    public int QuestionId { get; set; }
    public string? Answer { get; set; }
}

public sealed class ListingReportInput
{
    public int LostItemId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
}

public class ItemDetailViewModel
{
    public LostItem? Item { get; set; }
    public IReadOnlyList<RecommendedItem> Recommendations { get; set; } = [];
    public ChatMatch? ContactMatch { get; set; }
    public bool CanStartChat { get; set; }
    public string? CurrentUserId { get; set; }
    public ProtectedListingDto? ProtectedDetails { get; set; }
    public IReadOnlySet<int> VerifiedRecommendationIds { get; set; } = new HashSet<int>();
    public IReadOnlyDictionary<int, IReadOnlyList<VerificationQuestionDto>> VerificationQuestions { get; set; } =
        new Dictionary<int, IReadOnlyList<VerificationQuestionDto>>();
    public IReadOnlyList<ListingStatusHistory> StatusHistory { get; set; } = [];
}

public class MessagesViewModel
{
    public LostItem? Item { get; set; }
    public LostItem? MatchedItem { get; set; }
    public IReadOnlyList<ChatMatch> AvailableMatches { get; set; } = [];
}
