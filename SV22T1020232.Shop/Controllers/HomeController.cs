using Microsoft.AspNetCore.Mvc;
using SV22T1020232.Shop.Models;
using SV22T1020232.BusinessLayers;
using SV22T1020232.Models.Catalog;
using SV22T1020232.Models.Common;
using System.Diagnostics;

namespace SV22T1020232.Shop.Controllers
{
    /// <summary>
    /// Trang chủ Shop — hiển thị sản phẩm và danh mục nổi bật
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Trang chủ - Hiển thị sản phẩm nổi bật/mới nhất
        /// </summary>
        /// <returns>View trang chủ</returns>
        public async Task<IActionResult> Index()
        {
            try
            {
                var input = new SV22T1020232.Models.Catalog.ProductSearchInput
                {
                    Page = 1,
                    PageSize = 12,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };

                var result = await CatalogDataService.ListProductsAsync(input);

                var categoryInput = new PaginationSearchInput
                {
                    Page = 1,
                    PageSize = int.MaxValue,
                    SearchValue = ""
                };
                var categoryResult = await CatalogDataService.ListCategoriesAsync(categoryInput);

                ViewBag.Categories = categoryResult.DataItems;
                ViewBag.FeaturedProducts = result.DataItems;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page");
                ViewBag.FeaturedProducts = new List<Product>();
                ViewBag.Categories = new List<Category>();
                return View();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
