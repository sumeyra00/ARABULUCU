using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;
using ARABULUCU.Web.Models.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ARABULUCU.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ApplicationDbContext dbContext,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ILogger<AccountController> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null) =>
        View(new RegisterViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim()
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
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
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync();
            _logger.LogError(exception, "Could not save the new user profile for {Email}.", model.Email);
            ModelState.AddModelError(string.Empty, "Kayıt sırasında bir veritabanı hatası oluştu. Lütfen daha sonra tekrar deneyin.");

            return View("~/Views/Account/Register.cshtml", model);
        }

        TempData["SuccessMessage"] = "Kayıt başarılı. Giriş yapmak için e-posta adresiniz ve şifrenizi kullanın.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        var message = result.IsLockedOut
            ? "Çok fazla başarısız giriş denemesi. Lütfen daha sonra tekrar deneyin."
            : "E-posta veya şifre hatalı.";
        ModelState.AddModelError(string.Empty, message);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    private static string GetLocalizedError(IdentityError error)
    {
        return error.Code switch
        {
            "DuplicateEmail" or "DuplicateUserName" => "Bu e-posta adresiyle zaten bir hesap var.",
            "InvalidEmail" or "InvalidUserName" => "Geçerli bir e-posta adresi girin.",
            "PasswordTooShort" => "Şifre en az 8 karakter olmalıdır.",
            "PasswordRequiresNonAlphanumeric" => "Şifre en az bir harf veya rakam dışı karakter içermelidir.",
            "PasswordRequiresDigit" => "Şifre en az bir rakam içermelidir.",
            "PasswordRequiresLower" => "Şifre en az bir küçük harf içermelidir.",
            "PasswordRequiresUpper" => "Şifre en az bir büyük harf içermelidir.",
            "PasswordRequiresUniqueChars" => "Şifre yeterli sayıda farklı karakter içermelidir.",
            _ => error.Description
        };
    }
}
