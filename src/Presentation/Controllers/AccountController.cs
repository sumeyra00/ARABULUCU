using System.ComponentModel.DataAnnotations;
using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARABULUCU.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public AccountController(
        ApplicationDbContext dbContext,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Register() => View("~/Views/Account/Register.cshtml", new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!model.TermsAccepted)
        {
            ModelState.AddModelError(nameof(model.TermsAccepted), "Kullanım koşullarını ve KVKK metnini onaylamalısınız.");
        }

        if (!ModelState.IsValid)
        {
            return View("~/Views/Account/Register.cshtml", model);
        }

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email,
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            var duplicateAccount = result.Errors.Any(error =>
                error.Code is "DuplicateEmail" or "DuplicateUserName");

            if (duplicateAccount)
            {
                ModelState.AddModelError(string.Empty, "Bu e-posta adresiyle zaten bir hesap var.");
            }

            foreach (var error in result.Errors
                         .Where(error => error.Code is not ("DuplicateEmail" or "DuplicateUserName"))
                         .Select(error => error.Description)
                         .Distinct(StringComparer.Ordinal))
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View("~/Views/Account/Register.cshtml", model);
        }

        _dbContext.UserProfiles.Add(new UserProfile
        {
            Id = user.Id,
            DisplayName = model.DisplayName.Trim(),
            Email = user.Email!
        });
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login() => View("~/Views/Account/Login.cshtml", new LoginViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Account/Login.cshtml", model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
            return View("~/Views/Account/Login.cshtml", model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
        return View("~/Views/Account/Login.cshtml", model);
    }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Geçerli bir telefon numarası girin.")]
    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrar zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool TermsAccepted { get; set; }
}

public class LoginViewModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
