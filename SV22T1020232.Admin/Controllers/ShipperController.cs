using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020232.BusinessLayers;
using SV22T1020232.Models.Common;
using SV22T1020232.Models.Partner;

namespace SV22T1020232.Admin.Controllers
{
    /// <summary>
    /// Cung cấp các chức năng quản lý dữ liệu liên quan đến người giao hàng
    /// </summary>
    /// 
    [Authorize]
    public class ShipperController : Controller
    {
        private const string SHIPPER_SEARCH = "ShipperSearchInput";

        /// <summary>
        /// Nhập đầu vào tìm kiếm và hiển thị danh sách người giao hàng
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SHIPPER_SEARCH);
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
            var result = await PartnerDataService.ListShippersAsync(input);
            ApplicationContext.SetSessionData(SHIPPER_SEARCH, input);
            return View(result);
        }

        /// <summary>
        /// Bổ sung người giao hàng mới
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung người giao hàng";

            var model = new Shipper
            {
                ShipperID = 0
            };

            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin người giao hàng
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            var data = await PartnerDataService.GetShipperAsync(id);
            if (data == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Cập nhật thông tin người giao hàng";
            return View(data);
        }

        /// <summary>
        /// Lưu dữ liệu người giao hàng (bổ sung / cập nhật)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Shipper data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.ShipperName))
                    ModelState.AddModelError(nameof(data.ShipperName), "Tên người giao hàng không được để trống.");

                if (!ModelState.IsValid)
                {
                    ViewBag.Title = data.ShipperID == 0
                        ? "Bổ sung người giao hàng"
                        : "Cập nhật thông tin người giao hàng";

                    return View("Edit", data);
                }

                if (data.ShipperID == 0)
                {
                    data.ShipperID = await PartnerDataService.AddShipperAsync(data);
                    TempData["SuccessMessage"] = "Thêm người giao hàng thành công";
                    return RedirectToAction("Index");
                }
                else
                {
                    await PartnerDataService.UpdateShipperAsync(data);
                    TempData["SuccessMessage"] = "Cập nhật người giao hàng thành công";
                    return RedirectToAction("Index");
                }
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận. Vui lòng thử lại sau.");
                ViewBag.Title = data.ShipperID == 0
                    ? "Bổ sung người giao hàng"
                    : "Cập nhật thông tin người giao hàng";
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Xóa một người giao hàng
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            var data = await PartnerDataService.GetShipperAsync(id);
            if (data == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Xóa người giao hàng";
            return View(data);
        }

        /// <summary>
        /// Xác nhận xóa người giao hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelete(int shipperID)
        {
            try
            {
                var deleted = await PartnerDataService.DeleteShipperAsync(shipperID);
                if (!deleted)
                    TempData["ErrorMessage"] = "Người giao hàng đang được sử dụng nên không thể xóa.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Xóa người giao hàng không thành công. Vui lòng thử lại.";
            }
            return RedirectToAction("Index");
        }
    }
}
