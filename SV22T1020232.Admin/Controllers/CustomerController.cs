using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020232.BusinessLayers;
using SV22T1020232.Models.Common;
using SV22T1020232.Models.Partner;
using System.Text.RegularExpressions;

namespace SV22T1020232.Admin.Controllers
{
    /// <summary>
    /// Các chức năng quản lý dữ liệu lên quan đến khách hàng
    /// </summary>
    /// <returns></returns>
    /// 
    [Authorize]
    public class CustomerController : Controller
    {
        private const int PAGESIZE = 10;
        private const string CUSTOMER_SEARCH = "CustomerSearchInput";
        /// <summary>
        /// Nhập đầu vào tìm kiếm v  hi?n th? danh s ch kh ch h ng
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CUSTOMER_SEARCH);
            if (input == null)
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            return View(input);
        }
        /// <summary>
        /// Tìm kiếm và trả lời kết quả
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            await Task.Delay(1000);  // Chỉ để test
            var result = await PartnerDataService.ListCustomerAsync(input);
            ApplicationContext.SetSessionData(CUSTOMER_SEARCH, input);
            return View(result);
        }

        /// <summary>
        /// B? sung m?t khách hàng m?i
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung khách hàng";
            var model = new Customer()
            {
                CustomerID = 0
            };
            return View("Edit", model);
        }
        /// <summary>
        /// C?p nh?t th�ng tin m?t kh�ch h�ng
        /// </summary>
        /// <param name="id">M� kh�ch h�ng c?n c?p nh?t</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }
        /// <summary>
        /// Lưu dữ liệu vào CSDL, lưu và bảng Customer
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SaveData(Customer data)
        {
            try
            {
                ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật thông tin khách hàng";

                //Kiểm tra dữ liệu đầu vào
                //Sử dụng ModelState để lưu trữ và hiển thị thông báo lỗi
                if (string.IsNullOrWhiteSpace(data.CustomerName))
                    ModelState.AddModelError(nameof(data.CustomerName), "Vui lòng nhập tên");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Email không được để trống");
                else if (!IsValidEmail(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Email không đúng định dạng");
                else if (!(await PartnerDataService.ValidateCustomerEmailAsync(data.Email, data.CustomerID)))
                {
                    ModelState.AddModelError(nameof(data.Email), "Email đã có người sử dụng");
                }

                if (string.IsNullOrEmpty(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh thành");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                //Hiệu chỉnh dữ liệu (dạng mặc định)
                if (string.IsNullOrEmpty(data.ContactName))
                    data.ContactName = data.CustomerName;
                if (string.IsNullOrEmpty(data.Phone))
                    data.Phone = "";
                if (string.IsNullOrEmpty(data.Address))
                    data.Address = "";

                //Lưu vào CSDL
                if (data.CustomerID == 0)
                {
                    await PartnerDataService.AddCustomerAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateCustomerAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch
            {
                //Ghi log lỗi dựa vào thông tin trong ex (ex.Message và ex.StackTrace)
                ModelState.AddModelError("Error", "Hệ thống đang bận, vui lòng thử lại sau");
                return View("Edit", data);
            }


        }
        /// <summary>
        /// X�a m?t kh�ch h�ng
        /// </summary>
        /// <param name="id">M� kh�ch h�ng c?n x�a</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteCustomerAsync(id);
                return RedirectToAction("Index");
            }

            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            //Xử lý nút Xóa
            ViewBag.CanDelete = !(await PartnerDataService.IsUsedCustomerAsync(id));

            return View(model);
        }

        /// <summary>
        /// Thay đổi mật khẩu của một khách hàng
        /// </summary>
        /// <param name="id">Mã của khách hàng cần đổi mật khẩu</param>
        /// <returns></returns>
        public async Task<IActionResult> ChangePassword(int id)
        {
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveCustomerPassword(int id, string newPassword, string confirmPassword)
        {
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            if (string.IsNullOrWhiteSpace(newPassword))
                ModelState.AddModelError("NewPassword", "Vui lòng nhập mật khẩu mới");
            else if (newPassword != confirmPassword)
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp");

            if (!ModelState.IsValid)
                return View("ChangePassword", model);

            try
            {
                string hashedPassword = CryptHelper.HashMD5(newPassword);
                // Cần thêm method ChangeCustomerPasswordAsync vào PartnerDataService
                TempData["SuccessMessage"] = "Đổi mật khẩu khách hàng thành công";
                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Đổi mật khẩu không thành công. Vui lòng thử lại.");
                return View("ChangePassword", model);
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}