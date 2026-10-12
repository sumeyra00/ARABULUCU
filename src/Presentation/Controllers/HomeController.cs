using Microsoft.AspNetCore.Mvc;
using ARABULUCU.ENT.Entities;
using ARABULUCU.DAL.Repositories;
using System.Threading.Tasks;

namespace ARABULUCU.Web.Controllers
{
    /// <summary>
    /// Temel HomeController: uygulama açılış sayfasını sağlar.
    /// Controller, IItemRepository aracılığıyla veri çeker ve View'a gönderir.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly IItemRepository _items;

        /// <summary>
        /// Constructor: repository bağımlılığını alır (DI ile inject edilir).
        /// </summary>
        public HomeController(IItemRepository items)
        {
            _items = items;
        }

        /// <summary>
        /// Ana sayfa action'ı: tüm ilanları alır ve Index view'a gönderir.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var all = await _items.GetAllAsync();
            return View(all);
        }

        /// <summary>
        /// Gizlilik sayfası (statik view).
        /// </summary>
        public IActionResult Privacy()
        {
            return View();
        }
    }
}
