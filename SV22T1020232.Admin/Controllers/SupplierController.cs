using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020232.BusinessLayers;
using SV22T1020232.Models.Common;
using SV22T1020232.Models.Partner;

namespace SV22T1020232.Admin.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến quản lý dữ liệu nhà cung cấp
    /// </summary>
    /// 
    [Authorize]
    public class SupplierController : Controller
    {
        private const string SUPPLIER_SEARCH = "SupplierSearchInput";

        /// <summary>
        /// Nhập đầu vào tìm kiếm và hiển thị danh sách nhà cung cấp
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SUPPLIER_SEARCH);
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
            var result = await PartnerDataService.ListSupplierAsync(input);
            ApplicationContext.SetSessionData(SUPPLIER_SEARCH, input);
            return View(result);
        }

        /// <summary>
        /// Bổ sung nhà cung cấp mới
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhà cung cấp";

            var model = new Supplier
            {
                SupplierID = 0
            };

            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin nhà cung cấp
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            var data = await PartnerDataService.GetSupplierAsync(id);
            if (data == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Cập nhật thông tin nhà cung cấp";
            return View(data);
        }

        /// <summary>
        /// Lưu dữ liệu nhà cung cấp (bổ sung / cập nhật)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Supplier data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.SupplierName))
                    ModelState.AddModelError(nameof(data.SupplierName), "Tên nhà cung cấp không được để trống.");

                if (!ModelState.IsValid)
                {
                    ViewBag.Title = data.SupplierID == 0
                        ? "Bổ sung nhà cung cấp"
                        : "Cập nhật thông tin nhà cung cấp";

                    return View("Edit", data);
                }

                if (data.SupplierID == 0)
                {
                    data.SupplierID = await PartnerDataService.AddSupplierAsync(data);
                    TempData["SuccessMessage"] = "Thêm nhà cung cấp thành công";
                    return RedirectToAction("Index");
                }
                else
                {
                    await PartnerDataService.UpdateSupplierAsync(data);
                    TempData["SuccessMessage"] = "Cập nhật nhà cung cấp thành công";
                    return RedirectToAction("Index");
                }
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận. Vui lòng thử lại sau.");
                ViewBag.Title = data.SupplierID == 0
                    ? "Bổ sung nhà cung cấp"
                    : "Cập nhật thông tin nhà cung cấp";
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Xóa 1 nhà cung cấp
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            var data = await PartnerDataService.GetSupplierAsync(id);
            if (data == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Xóa nhà cung cấp";
            return View(data);
        }

        /// <summary>
        /// Xác nhận xóa nhà cung cấp
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelete(int supplierID)
        {
            try
            {
                var deleted = await PartnerDataService.DeleteSupplierAsync(supplierID);
                if (!deleted)
                    TempData["ErrorMessage"] = "Nhà cung cấp đang được sử dụng nên không thể xóa.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Xóa nhà cung cấp không thành công. Vui lòng thử lại.";
            }
            return RedirectToAction("Index");
        }
    }
}