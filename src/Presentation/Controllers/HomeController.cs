using Microsoft.AspNetCore.Mvc;

namespace ARABULUCU.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {

            return View();
        }
        public IActionResult Login()
        {
            return View("~/Views/Account/Login.cshtml");
        }
        public IActionResult Register()
        {
            return View("~/Views/Account/Register.cshtml");
        }
        public IActionResult KayipOlustur()
        {
            return View("~/Views/Ilan/KayipOlustur.cshtml");
        }
        public IActionResult BulunanOlustur()
        {
            return View("~/Views/Ilan/BulunanOlustur.cshtml");
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