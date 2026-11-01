#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ARABULUCU.BLL.DTOs;
using ARABULUCU.BLL.Services;
using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;
using ARABULUCU.Web.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace ARABULUCU.Tests;

public sealed class ItemDetailRecommendationTests
{
    [Theory]
    [InlineData(false, "Bulunan")]
    [InlineData(true, "Kayıp")]
    public async Task Detay_RendersOppositeTypeBadgeScoreAndChatLink(bool sourceIsFound, string candidateType)
    {
        var (result, httpContext) = await GetDetailResultAsync(sourceIsFound, delivered: false, itemFound: sourceIsFound);

        var viewModel = Assert.IsType<ItemDetailViewModel>(result.Model);
        Assert.True(viewModel.CanStartChat);
        var html = await RenderDetailViewAsync(viewModel, httpContext);

        Assert.Contains(candidateType, html);
        Assert.Matches(@"73[,.]5% benzer", html);
        Assert.Contains("Eşleşme sohbetini aç", html);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, true, true)]
    public async Task Detay_HidesChatLinkForClosedOrDeliveredSource(
        bool sourceIsFound,
        bool delivered,
        bool itemFound)
    {
        var (result, httpContext) = await GetDetailResultAsync(sourceIsFound, delivered, itemFound);

        var viewModel = Assert.IsType<ItemDetailViewModel>(result.Model);
        Assert.False(viewModel.CanStartChat);
        var html = await RenderDetailViewAsync(viewModel, httpContext);

        Assert.Matches(@"73[,.]5% benzer", html);
        Assert.DoesNotContain("Eşleşme sohbetini aç", html);
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, true)]
    public async Task Detay_HidesChatLinkForClosedOrDeliveredCandidate(
        bool sourceIsFound,
        bool candidateDelivered,
        bool candidateItemFound)
    {
        var (result, httpContext) = await GetDetailResultAsync(
            sourceIsFound,
            delivered: false,
            itemFound: sourceIsFound,
            candidateDelivered: candidateDelivered,
            candidateItemFound: candidateItemFound);

        var viewModel = Assert.IsType<ItemDetailViewModel>(result.Model);
        Assert.True(viewModel.CanStartChat);
        var html = await RenderDetailViewAsync(viewModel, httpContext);

        Assert.DoesNotContain("Eşleşme sohbetini aç", html);
    }

    [Fact]
    public async Task Detay_HidesChatLinkForAnonymousVisitor()
    {
        var (result, httpContext) = await GetDetailResultAsync(
            sourceIsFound: false,
            delivered: false,
            itemFound: false,
            authenticated: false);

        var viewModel = Assert.IsType<ItemDetailViewModel>(result.Model);
        Assert.True(viewModel.CanStartChat);
        Assert.Null(viewModel.CurrentUserId);
        var html = await RenderDetailViewAsync(viewModel, httpContext);

        Assert.DoesNotContain("Eşleşme sohbetini aç", html);
    }

    [Fact]
    public async Task Detay_HtmlDoesNotExposeHiddenFeatures()
    {
        var (result, httpContext) = await GetDetailResultAsync(sourceIsFound: false, delivered: false, itemFound: false);
        var viewModel = Assert.IsType<ItemDetailViewModel>(result.Model);
        var html = await RenderDetailViewAsync(viewModel, httpContext);

        Assert.Contains("Visible color", html);
        Assert.DoesNotContain("Secret question", html);
        Assert.DoesNotContain("Private answer", html);
    }

    [Fact]
    public async Task Detay_HtmlDoesNotExposeCandidateHiddenFeatures()
    {
        var (result, httpContext) = await GetDetailResultAsync(
            sourceIsFound: false,
            delivered: false,
            itemFound: false,
            candidateHasHiddenFeature: true);
        var viewModel = Assert.IsType<ItemDetailViewModel>(result.Model);
        var html = await RenderDetailViewAsync(viewModel, httpContext);

        Assert.DoesNotContain("Candidate secret key", html);
        Assert.DoesNotContain("Candidate secret value", html);
    }

    [Fact]
    public async Task Detaj_FoundItemRendersPublicDetailsAndVisibleFeaturesOnly()
    {
        var (result, httpContext) = await GetDetailResultAsync(
            sourceIsFound: true,
            delivered: false,
            itemFound: true);
        var viewModel = Assert.IsType<ItemDetailViewModel>(result.Model);
        var html = await RenderDetailViewAsync(viewModel, httpContext);

        Assert.Contains("Item description", html);
        Assert.Contains("Wallets", html);
        Assert.Contains("Library", html);
        Assert.Contains("Visible color", html);
        Assert.DoesNotContain("Secret question", html);
        Assert.DoesNotContain("Private answer", html);
    }

    private static async Task<(ViewResult Result, HttpContext HttpContext)> GetDetailResultAsync(
        bool sourceIsFound,
        bool delivered,
        bool itemFound,
        bool candidateDelivered = false,
        bool candidateItemFound = false,
        bool authenticated = true,
        bool candidateHasHiddenFeature = false)
    {
        var source = Item(1, "source", sourceIsFound, delivered, itemFound);
        source.Features.Add(new LostItemFeature { Key = "Color", Value = "Visible color", IsHidden = false });
        source.Features.Add(new LostItemFeature { Key = "SecurityQuestion", Value = "Secret question", IsHidden = true });
        source.Features.Add(new LostItemFeature { Key = "SecurityAnswerHash", Value = "Private answer", IsHidden = true });
        var candidate = Item(2, "other", !sourceIsFound, candidateDelivered, candidateItemFound);
        if (candidateHasHiddenFeature)
        {
            candidate.Features.Add(new LostItemFeature
            {
                Key = "Candidate secret key",
                Value = "Candidate secret value",
                IsHidden = true
            });
        }

        var listingService = new TestListingService(source, candidate);

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        var httpContext = new DefaultHttpContext
        {
            User = authenticated
                ? new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, source.UserId)],
                    "test"))
                : new ClaimsPrincipal(new ClaimsIdentity())
        };
        var controller = new HomeController(db, new TestWebHostEnvironment(), listingService)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var result = Assert.IsType<ViewResult>(await controller.Detay(source.Id, null));
        return (result, httpContext);
    }

    private static async Task<string> RenderDetailViewAsync(ItemDetailViewModel model, HttpContext httpContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<DiagnosticSource>(new DiagnosticListener("ItemDetailRecommendationTests"));
        services.AddSingleton(new DiagnosticListener("ItemDetailRecommendationTests"));
        services.AddSingleton<IUrlHelperFactory, TestUrlHelperFactory>();
        services.AddControllersWithViews().AddApplicationPart(typeof(HomeController).Assembly);
        using var provider = services.BuildServiceProvider();
        httpContext.RequestServices = provider;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var engine = provider.GetRequiredService<IRazorViewEngine>();
        var viewResult = engine.GetView(null, "/Views/Ilan/Detay.cshtml", isMainPage: false);
        Assert.True(viewResult.Success, string.Join(Environment.NewLine, viewResult.SearchedLocations ?? []));

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model
        };
        var tempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        await using var writer = new StringWriter(new StringBuilder());
        var viewContext = new ViewContext(
            actionContext,
            viewResult.View!,
            viewData,
            tempData,
            writer,
            new HtmlHelperOptions());

        await viewResult.View!.RenderAsync(viewContext);
        return writer.ToString();
    }

    private static LostItem Item(int id, string userId, bool isFound, bool delivered, bool itemFound) =>
        new()
        {
            Id = id,
            UserId = userId,
            Title = isFound ? "Found item" : "Lost item",
            Description = "Item description",
            Location = "Library",
            Category = new Category { Name = "Wallets" },
            IsFound = isFound,
            IsDelivered = delivered,
            IsItemFound = itemFound,
            EventDate = DateTime.Today
        };

    private sealed class TestListingService(LostItem source, LostItem candidate) : ILostItemListingService
    {
        public Task<ItemSearchResult> SearchAsync(ItemSearchCriteria criteria) =>
            Task.FromResult(new ItemSearchResult());

        public Task<LostItem?> GetByIdAsync(int id) =>
            Task.FromResult<LostItem?>(id == source.Id ? source : null);

        public Task<IReadOnlyList<RecommendedItem>> GetRecommendationsAsync(LostItem item, int limit = 5) =>
            Task.FromResult<IReadOnlyList<RecommendedItem>>(
                [new RecommendedItem { Item = candidate, SimilarityPercent = 73.5m }]);

        public Task<bool> CanChatAsync(string userId, int itemId, int matchedItemId) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<ChatMatch>> GetChatMatchesAsync(string userId) =>
            Task.FromResult<IReadOnlyList<ChatMatch>>([]);
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

    private sealed class TestUrlHelperFactory : IUrlHelperFactory
    {
        public IUrlHelper GetUrlHelper(ActionContext context) => new TestUrlHelper(context);
    }

    private sealed class TestUrlHelper(ActionContext actionContext) : IUrlHelper
    {
        public ActionContext ActionContext { get; } = actionContext;

        public string? Action(UrlActionContext actionContext) => "/";

        public string? Content(string? contentPath) => contentPath;

        public bool IsLocalUrl(string? url) => url?.StartsWith("/", StringComparison.Ordinal) == true;

        public string? Link(string? routeName, object? values) => "/";

        public string? RouteUrl(UrlRouteContext routeContext) => "/";
    }
}
