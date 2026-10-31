#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using ARABULUCU.BLL.DTOs;
using ARABULUCU.BLL.Services;
using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;
using ARABULUCU.Web.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace ARABULUCU.Tests;

public sealed class FoundItemFeatureVisibilityTests
{
    [Fact]
    public async Task BulunanOlustur_SavesSelectedFeaturesWithRequestedVisibility()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.UserProfiles.Add(new UserProfile
        {
            Id = "finder",
            DisplayName = "Finder",
            Email = "finder@example.com"
        });
        await dbContext.SaveChangesAsync();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "finder")],
                "test"))
        };
        var controller = new HomeController(dbContext, new TestWebHostEnvironment(), new TestListingService())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };

        var result = await controller.BulunanOlustur(new ItemReportViewModel
        {
            Title = "Mavi termos",
            Category = "diger",
            ItemDate = new DateTime(2026, 10, 31, 12, 0, 0),
            CampusArea = "yemekhane",
            SecurityQuestion = "Termosun içindeki etiket ne renk?",
            ItemStatus = "with_me",
            Brand = "TermosCo",
            HideBrand = true,
            Model = "500 ml",
            HideModel = false,
            Color = "Mavi",
            HideColor = true
        });

        Assert.IsType<RedirectToActionResult>(result);
        var features = await dbContext.LostItemFeatures
            .AsNoTracking()
            .ToDictionaryAsync(feature => feature.Key, feature => feature);

        Assert.True(features["Brand"].IsHidden);
        Assert.False(features["Model"].IsHidden);
        Assert.True(features["Color"].IsHidden);
        Assert.True(features["SecurityQuestion"].IsHidden);
    }

    private sealed class TestListingService : ILostItemListingService
    {
        public Task<ItemSearchResult> SearchAsync(ItemSearchCriteria criteria) =>
            Task.FromResult(new ItemSearchResult());

        public Task<LostItem?> GetByIdAsync(int id) =>
            Task.FromResult<LostItem?>(null);

        public Task<IReadOnlyList<RecommendedItem>> GetRecommendationsAsync(LostItem item, int limit = 5) =>
            Task.FromResult<IReadOnlyList<RecommendedItem>>([]);
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "ARABULUCU.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
