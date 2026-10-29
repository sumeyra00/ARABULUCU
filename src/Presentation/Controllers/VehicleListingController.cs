using System.Security.Claims;
using ARABULUCU.BLL.DTOs;
using ARABULUCU.BLL.Services;
using ARABULUCU.ENT.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ARABULUCU.Web.Controllers;

[Authorize]
public class VehicleListingController : Controller
{
    private readonly IVehicleListingService _listingService;

    public VehicleListingController(IVehicleListingService listingService)
    {
        _listingService = listingService;
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new CreateVehicleListingViewModel();
        await LoadFeaturesAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateVehicleListingViewModel model,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await LoadFeaturesAsync(model, cancellationToken);
            return View(model);
        }

        var result = await _listingService.CreateAsync(model, userId, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(model.SelectedFeatureIds), result.ErrorMessage!);
            await LoadFeaturesAsync(model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = "Araç ilanınız ve seçtiğiniz özellikler kaydedildi.";
        return RedirectToAction(nameof(Create));
    }

    private async Task LoadFeaturesAsync(
        CreateVehicleListingViewModel model,
        CancellationToken cancellationToken)
    {
        model.Features = await _listingService.GetFeaturesAsync(cancellationToken);
    }
}

public sealed class CreateVehicleListingViewModel : CreateListingDto
{
    [BindNever]
    public IReadOnlyList<VehicleFeature> Features { get; set; } = [];
}
