using ARABULUCU.DAL.Data;
using ARABULUCU.DAL.Entities;
using ARABULUCU.ENT.Entities;
using ARABULUCU.Web.Models.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARABULUCU.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                PhoneNumber = model.PhoneNumber?.Trim(),
                DisplayName = model.FullName.Trim()
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
                DisplayName = user.DisplayName,
                Email = email
            });

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["RegisterSuccess"] = "Kaydınız oluşturuldu.";
            return RedirectToAction(nameof(Register));
        }

        private static string GetLocalizedError(IdentityError error)
        {
            return error.Code switch
            {
                "DuplicateEmail" or "DuplicateUserName" => "Bu e-posta adresiyle zaten bir hesap var.",
                "InvalidEmail" or "InvalidUserName" => "Geçerli bir e-posta adresi girin.",
                "PasswordTooShort" => "Şifre en az 6 karakter olmalıdır.",
                "PasswordRequiresNonAlphanumeric" => "Şifre en az bir harf veya rakam dışı karakter içermelidir.",
                "PasswordRequiresDigit" => "Şifre en az bir rakam içermelidir.",
                "PasswordRequiresLower" => "Şifre en az bir küçük harf içermelidir.",
                "PasswordRequiresUpper" => "Şifre en az bir büyük harf içermelidir.",
                "PasswordRequiresUniqueChars" => "Şifre yeterli sayıda farklı karakter içermelidir.",
                _ => error.Description
            };
        }
    }
}
