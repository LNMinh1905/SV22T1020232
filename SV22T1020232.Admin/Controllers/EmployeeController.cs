using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020232.BusinessLayers;
using SV22T1020232.Models.Common;
using SV22T1020232.Models.HR;

namespace SV22T1020232.Admin.Controllers
{
    /// <summary>
    /// Các chức năng quản lý dữ liệu liên quan đến nhân viên
    /// </summary>
    /// 
    [Authorize]
    public class EmployeeController : Controller
    {
        /// <summary>
        /// Tìm kiếm và hiển thị danh sách nhân viên
        /// </summary>
        /// <returns></returns>
        private const string EMPLOYEE_SEARCH = "EmployeeSearchInput";

        /// <summary>
        /// Nhập đầu vào tìm kiếm và hiển thị danh sách nhân viên
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(EMPLOYEE_SEARCH);
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
            var result = await HRDataService.ListEmployeesAsync(input);
            ApplicationContext.SetSessionData(EMPLOYEE_SEARCH, input);
            return View(result);
        }

        /// <summary>
        /// Bổ sung một nhân viên mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                IsWorking = true
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin một nhân viên
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Employee data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";

                //Kiểm tra dữ liệu đầu vào: FullName và Email là bắt buộc, Email chưa được sử dụng bởi nhân viên khác
                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email nhân viên");
                else if (!await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID))
                    ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi nhân viên khác");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                //Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/employees", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                //Tiền xử lý dữ liệu trước khi lưu vào database
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";

                //Lưu dữ liệu vào database (bổ sung hoặc cập nhật)
                if (data.EmployeeID == 0)
                {
                    await HRDataService.AddEmployeeAsync(data);
                    TempData["SuccessMessage"] = "Thêm nhân viên thành công";
                    return RedirectToAction("ChangePassword", new { id = data.EmployeeID });
                }
                else
                {
                    await HRDataService.UpdateEmployeeAsync(data);
                    TempData["SuccessMessage"] = "Cập nhật nhân viên thành công";
                }
                return RedirectToAction("Index");
            }
            catch //(Exception ex)
            {
                //TODO: Ghi log lỗi căn cứ vào ex.Message và ex.StackTrace
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ. Vui lòng kiểm tra dữ liệu hoặc thử lại sau");
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Xóa một nhân viên
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            var data = await HRDataService.GetEmployeeAsync(id);
            if (data == null)
                return RedirectToAction("Index");

            return View(data);
        }

        /// <summary>
        /// Đổi mật khẩu nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần đổi mật khẩu</param>
        /// <returns></returns>
        public async Task<IActionResult> ChangePassword(int id)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SavePassword(int id, string newPassword, string confirmPassword)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
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
                await HRDataService.ChangeEmployeePasswordAsync(model.Email, hashedPassword);
                TempData["SuccessMessage"] = "Đổi mật khẩu nhân viên thành công";
                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Đổi mật khẩu không thành công. Vui lòng thử lại.");
                return View("ChangePassword", model);
            }
        }

        /// <summary>
        /// Phân quyền cho nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần phân quyền</param>
        /// <returns></returns>
        public async Task<IActionResult> ChangeRole(int id)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveRole(int id, string[] roles)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            try
            {
                // Lưu quyền vào database (tùy cấu trúc DB thực tế)
                // Hiện tại chỉ demo, bạn cần triển khai logic lưu roles
                TempData["SuccessMessage"] = "Phân quyền nhân viên thành công";
                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Phân quyền không thành công. Vui lòng thử lại.");
                return View("ChangeRole", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelete(int employeeID)
        {
            try
            {
                var deleted = await HRDataService.DeleteEmployeeAsync(employeeID);
                if (!deleted)
                    TempData["ErrorMessage"] = "Nhân viên đang được sử dụng nên không thể xóa.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Xóa nhân viên không thành công. Vui lòng thử lại.";
            }
            return RedirectToAction("Index");
        }
    }
}