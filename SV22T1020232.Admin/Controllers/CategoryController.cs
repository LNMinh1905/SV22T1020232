using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020232.BusinessLayers;
using SV22T1020232.Models.Catalog;
using SV22T1020232.Models.Common;

namespace SV22T1020232.Admin.Controllers
{
    /// <summary>
    /// Các chức năng quản lý dữ liệu liên quan đến loại hàng
    /// </summary>
    /// 
    [Authorize]
    public class CategoryController : Controller
    {
        private const string CATEGORY_SEARCH = "CategorySearchInput";

        /// <summary>
        /// Nhập đầu vào tìm kiếm và hiển thị danh sách loại hàng
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CATEGORY_SEARCH);
            if (input == null)
                input = new PaginationSearchInput
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và trả lời kết quả (AJAX)
        /// </summary>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await CatalogDataService.ListCategoriesAsync(input);
            ApplicationContext.SetSessionData(CATEGORY_SEARCH, input);
            return View(result);
        }

        /// <summary>
        /// Bổ sung loại hàng mới
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung loại hàng";

            var model = new Category
            {
                CategoryID = 0
            };

            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin 1 loại hàng
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            var data = await CatalogDataService.GetCategoryAsync(id);
            if (data == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Cập nhật thông tin loại hàng";
            return View(data);
        }

        /// <summary>
        /// Lưu dữ liệu loại hàng (bổ sung / cập nhật)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Category data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.CategoryName))
                    ModelState.AddModelError(nameof(data.CategoryName), "Tên loại hàng không được để trống.");

                if (!ModelState.IsValid)
                {
                    ViewBag.Title = data.CategoryID == 0
                        ? "Bổ sung loại hàng"
                        : "Cập nhật thông tin loại hàng";

                    return View("Edit", data);
                }

                if (data.CategoryID == 0)
                {
                    data.CategoryID = await CatalogDataService.AddCategoryAsync(data);
                    TempData["SuccessMessage"] = "Thêm loại hàng thành công";
                    return RedirectToAction("Index");
                }
                else
                {
                    await CatalogDataService.UpdateCategoryAsync(data);
                    TempData["SuccessMessage"] = "Cập nhật loại hàng thành công";
                    return RedirectToAction("Index");
                }
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận. Vui lòng thử lại sau.");
                ViewBag.Title = data.CategoryID == 0
                    ? "Bổ sung loại hàng"
                    : "Cập nhật thông tin loại hàng";
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Xóa 1 loại hàng
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            var data = await CatalogDataService.GetCategoryAsync(id);
            if (data == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Xóa loại hàng";
            return View(data);
        }

        /// <summary>
        /// Xác nhận xóa loại hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelete(int categoryID)
        {
            try
            {
                var deleted = await CatalogDataService.DeleteCategoryAsync(categoryID);
                if (!deleted)
                    TempData["ErrorMessage"] = "Loại hàng đang được sử dụng nên không thể xóa.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Xóa loại hàng không thành công. Vui lòng thử lại.";
            }
            return RedirectToAction("Index");
        }
    }
}
