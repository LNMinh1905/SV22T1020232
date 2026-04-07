using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020232.BusinessLayers;

namespace SV22T1020232.Admin.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến tài khoản
    /// </summary>
    /// 
    [Authorize]
    public class AccountController : Controller
    {
        /// <summary>
        /// Đăng nhập
        /// </summary>
        /// <returns></returns>
        /// 
        [HttpGet]
        [AllowAnonymous]

        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]

        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.Username = username;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ email và mật khẩu.");
                return View();
            }

            string hashedPassword = CryptHelper.HashMD5(password);

            var userAccount = await SecurityDataService.AuthorizeEmployeeAsync(username, hashedPassword);

            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View();
            }

            //Tạo thông tin để ghi trên giấy chứng nhận
            var userData = new WebUserData()
            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName = userAccount.DisplayName,
                Email = userAccount.Email,
                Photo = userAccount.Photo,
                Roles = string.IsNullOrWhiteSpace(userAccount.RoleNames)
                    ? new List<string> { WebUserRoles.Sales }
                    : userAccount.RoleNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            };

            // tạo giấy chứng nhận
            var principal = userData.CreatePrincipal();

            // cấp giấy chứng nhận
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Đăng xuất
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword))
                ModelState.AddModelError("CurrentPassword", "Vui lòng nhập mật khẩu hiện tại");
            
            if (string.IsNullOrWhiteSpace(newPassword))
                ModelState.AddModelError("NewPassword", "Vui lòng nhập mật khẩu mới");
            else if (newPassword != confirmPassword)
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp");

            if (!ModelState.IsValid)
                return View();

            try
            {
                var user = User.GetUserData();
                if (user == null || string.IsNullOrWhiteSpace(user.Email))
                {
                    ModelState.AddModelError(string.Empty, "Không xác định được tài khoản.");
                    return View();
                }

                // Kiểm tra mật khẩu hiện tại
                string hashedCurrentPassword = CryptHelper.HashMD5(currentPassword);
                var userAccount = await SecurityDataService.AuthorizeEmployeeAsync(user.Email, hashedCurrentPassword);
                
                if (userAccount == null)
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
                    return View();
                }

                // Đổi mật khẩu
                string hashedNewPassword = CryptHelper.HashMD5(newPassword);
                await SecurityDataService.ChangeEmployeePasswordAsync(user.Email, hashedNewPassword);
                
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công";
                return RedirectToAction("Index", "Home");
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Đổi mật khẩu không thành công. Vui lòng thử lại.");
                return View();
            }
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}