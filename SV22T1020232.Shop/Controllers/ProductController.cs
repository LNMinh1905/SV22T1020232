using Microsoft.AspNetCore.Mvc;
using SV22T1020232.BusinessLayers;
using SV22T1020232.Models.Catalog;
using SV22T1020232.Models.Common;

namespace SV22T1020232.Shop.Controllers
{
    /// <summary>
    /// Hiển thị danh sách, tìm kiếm và chi tiết sản phẩm cho khách hàng
    /// </summary>
    public class ProductController : Controller
    {
        private const int PAGE_SIZE = 12;

        /// <summary>
        /// Danh sách sản phẩm với tìm kiếm kết hợp (tên, category, giá) và phân trang
        /// </summary>
        public async Task<IActionResult> Index(ProductSearchInput input)
        {
            try
            {
                if (input.Page < 1) input.Page = 1;
                input.PageSize = PAGE_SIZE;

                var categoryInput = new PaginationSearchInput { Page = 1, PageSize = int.MaxValue };

                var productsTask   = CatalogDataService.ListProductsAsync(input);
                var categoriesTask = CatalogDataService.ListCategoriesAsync(categoryInput);

                await Task.WhenAll(productsTask, categoriesTask);

                ViewBag.Categories  = categoriesTask.Result.DataItems;
                ViewBag.SearchInput = input;

                return View(productsTask.Result);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi tải danh sách sản phẩm: {ex.Message}";
                return View(new PagedResult<Product>());
            }
        }

        /// <summary>
        /// Chi tiết sản phẩm — load thông tin, hình ảnh và thuộc tính liên quan
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return RedirectToAction("Index");

            try
            {
                var productTask    = CatalogDataService.GetProductAsync(id);
                var photosTask     = CatalogDataService.ListProductPhotosAsync(id);
                var attributesTask = CatalogDataService.ListProductAttributesAsync(id);

                await Task.WhenAll(productTask, photosTask, attributesTask);

                var product = productTask.Result;
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại hoặc đã bị gỡ.";
                    return RedirectToAction("Index");
                }

                ViewBag.Photos     = photosTask.Result;
                ViewBag.Attributes = attributesTask.Result;

                return View(product);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi tải chi tiết sản phẩm: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}
