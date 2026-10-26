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
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, GetLocalizedError(error));
            }

            return View(model);
        }

        _dbContext.UserProfiles.Add(new UserProfile
        {
            Id = user.Id,
            DisplayName = model.FullName.Trim(),
            Email = email
        });
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["RegisterSuccess"] = "Kaydınız oluşturuldu. E-posta adresiniz ve şifrenizle giriş yapabilirsiniz.";
        return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
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
