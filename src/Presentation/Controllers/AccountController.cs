using ARABULUCU.DAL.Data;
using ARABULUCU.ENT.Entities;
using ARABULUCU.Web.Models.Account;
using ARABULUCU.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace ARABULUCU.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly ILogger<AccountController> _logger;
    private readonly IAccountEmailSender _emailSender;

    public AccountController(
        ApplicationDbContext dbContext,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        ILogger<AccountController> logger,
        IAccountEmailSender emailSender)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _emailSender = emailSender;
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
                             .Select(GetLocalizedError)
                             .Distinct(StringComparer.Ordinal))
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return View("~/Views/Account/Register.cshtml", model);
            }

            _dbContext.UserProfiles.Add(new UserProfile
            {
                Id = user.Id,
                DisplayName = model.FullName.Trim(),
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
        return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetUrl = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { email = user.Email, code = encodedToken },
                Request.Scheme);

            if (resetUrl is null)
            {
                throw new InvalidOperationException("Could not generate the password reset URL.");
            }

            try
            {
                await _emailSender.SendPasswordResetLinkAsync(user.Email!, resetUrl);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not send a password reset email to {Email}.", user.Email);
            }
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    public IActionResult ResetPassword(string? email, string? code)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            return BadRequest("Şifre sıfırlama bağlantısı geçersiz veya eksik.");
        }

        try
        {
            WebEncoders.Base64UrlDecode(code);
            return View(new ResetPasswordViewModel { Email = email, Code = code });
        }
        catch (FormatException)
        {
            return BadRequest("Şifre sıfırlama bağlantısı geçersiz veya eksik.");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
        }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.");
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.");
            return View(model);
        }

        var result = await _userManager.ResetPasswordAsync(user, token, model.Password);
        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "Password reset failed for user {UserId}. Identity errors: {ErrorCodes}",
                user.Id,
                string.Join(", ", result.Errors.Select(error => error.Code)));

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Code == "InvalidToken"
                        ? "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş."
                        : GetLocalizedError(error));
            }

            return View(model);
        }

        TempData["SuccessMessage"] = "Şifreniz yenilendi. Yeni şifrenizle giriş yapabilirsiniz.";
        return RedirectToAction(nameof(Login));
    }

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
