using Microsoft.AspNetCore.Mvc;
using ARABULUCU.ENT.Entities;
using ARABULUCU.DAL.Repositories;
using System.Threading.Tasks;

namespace ARABULUCU.Web.Controllers
{
    // Temel HomeController: uygulama açılış sayfasını sağlar
    public class HomeController : Controller
    {
        private readonly IItemRepository _items;

        public HomeController(IItemRepository items)
        {
            _items = items;
        }

        public async Task<IActionResult> Index()
        {
            var all = await _items.GetAllAsync();
            return View(all);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
