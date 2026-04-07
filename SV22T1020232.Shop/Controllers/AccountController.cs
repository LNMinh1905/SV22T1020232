using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020232.BusinessLayers;
using SV22T1020232.Models.Partner;
using SV22T1020232.Shop.Models;

namespace SV22T1020232.Shop.Controllers
{
    /// <summary>
    /// Controller xử lý tài khoản khách hàng: Đăng ký, Đăng nhập, Hồ sơ, Đổi mật khẩu.
    /// </summary>
    public class AccountController : Controller
    {
        private const string AvatarFolder = "images/customers";
        private const long   MaxAvatarSize = 2 * 1024 * 1024;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                string hashedPassword = CryptHelper.HashMD5(model.Password);
                var userAccount = await SecurityDataService.AuthorizeCustomerAsync(model.Email.Trim(), hashedPassword);

                if (userAccount == null)
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng. Vui lòng kiểm tra lại.");
                    return View(model);
                }

                var userData = new WebUserData
                {
                    CustomerId   = userAccount.UserId,
                    CustomerName = userAccount.DisplayName,
                    Email        = userAccount.Email,
                    Photo        = userAccount.Photo ?? ""
                };

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(4)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    userData.CreatePrincipal(),
                    authProperties
                );

                TempData["SuccessMessage"] = $"Chào mừng trở lại, {userData.CustomerName}! 🎉";

                int cid = int.Parse(userAccount.UserId);
                var customerInfo = await PartnerDataService.GetCustomerAsync(cid);
                bool isIncomplete = customerInfo == null
                    || string.IsNullOrWhiteSpace(customerInfo.Phone)
                    || string.IsNullOrWhiteSpace(customerInfo.Address)
                    || string.IsNullOrWhiteSpace(customerInfo.Province);

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);

                if (isIncomplete)
                {
                    TempData["InfoMessage"] = "Vui lòng cập nhật đầy đủ thông tin cá nhân trước khi mua sắm.";
                    return RedirectToAction("Profile");
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
                return View(model);
            }
        }

        /// <summary>
        /// Đăng xuất — xóa cookie xác thực và redirect về trang chủ
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Đã đăng xuất thành công!";
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Hiển thị form đăng ký khách hàng mới (GET)
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            bool emailValid = await PartnerDataService.ValidateCustomerEmailAsync(model.Email.Trim(), 0);
            if (!emailValid)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng. Vui lòng dùng email khác.");
                return View(model);
            }

            string hashedPassword = CryptHelper.HashMD5(model.Password);

            var customer = new Customer
            {
                CustomerName = model.CustomerName.Trim(),
                ContactName  = (model.ContactName ?? model.CustomerName).Trim(),
                Email        = model.Email.Trim().ToLower(),
                Phone        = model.Phone?.Trim() ?? "",
                Address      = model.Address?.Trim() ?? "",
                Province     = model.Province?.Trim() ?? "",
                IsLocked     = false
            };

            try
            {
                int customerId = await PartnerDataService.AddCustomerAsync(customer);

                if (customerId <= 0)
                {
                    ModelState.AddModelError("", "Đăng ký thất bại. Vui lòng thử lại sau.");
                    return View(model);
                }

                await SecurityDataService.ChangeCustomerPasswordAsync(customer.Email, hashedPassword);

                var userData = new WebUserData
                {
                    CustomerId   = customerId.ToString(),
                    CustomerName = customer.CustomerName,
                    Email        = customer.Email,
                    Photo        = ""
                };

                TempData["SuccessMessage"] = "Tài khoản đã được tạo thành công! Vui lòng đăng nhập để tiếp tục.";
                return RedirectToAction("Login");
            }
            catch (ArgumentException argEx)
            {
                ModelState.AddModelError("", argEx.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
                return View(model);
            }
        }

        /// <summary>
        /// Hiển thị trang hồ sơ cá nhân của khách hàng (GET)
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login");

            try
            {
                int customerId = int.Parse(userData.CustomerId!);
                var customer = await PartnerDataService.GetCustomerAsync(customerId);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                    return RedirectToAction("Index", "Home");
                }

                var model = MapToProfileViewModel(customer, userData.Photo);
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Cập nhật thông tin hồ sơ cá nhân và ảnh đại diện (POST)
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            ModelState.Remove("AvatarFile");
            ModelState.Remove("Photo");

            if (!ModelState.IsValid)
                return View(model);

            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login");

            try
            {
                int customerId = int.Parse(userData.CustomerId!);
                var customer = await PartnerDataService.GetCustomerAsync(customerId);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                    return View(model);
                }

                string newPhoto = userData.Photo ?? "";

                if (model.AvatarFile != null && model.AvatarFile.Length > 0)
                {
                    var validateResult = ValidateAvatarFile(model.AvatarFile);
                    if (validateResult != null)
                    {
                        ModelState.AddModelError("AvatarFile", validateResult);
                        model.Photo = newPhoto;
                        return View(model);
                    }

                    newPhoto = await SaveAvatarFileAsync(model.AvatarFile, customerId);
                }

                customer.CustomerName = model.CustomerName.Trim();
                customer.ContactName  = (model.ContactName ?? model.CustomerName).Trim();
                customer.Phone        = model.Phone?.Trim() ?? "";
                customer.Address      = model.Address?.Trim() ?? "";
                customer.Province     = model.Province?.Trim() ?? "";

                bool success = await PartnerDataService.UpdateCustomerAsync(customer);

                if (success)
                {
                    var updatedUserData = new WebUserData
                    {
                        CustomerId   = userData.CustomerId,
                        CustomerName = customer.CustomerName,
                        Email        = customer.Email,
                        Photo        = newPhoto
                    };
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        updatedUserData.CreatePrincipal()
                    );

                    TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
                    return RedirectToAction("Profile");
                }

                ModelState.AddModelError("", "Cập nhật thất bại. Vui lòng thử lại.");
                model.Photo = newPhoto;
                return View(model);
            }
            catch (ArgumentException argEx)
            {
                ModelState.AddModelError("", argEx.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
                return View(model);
            }
        }

        /// <summary>
        /// Hiển thị form đổi mật khẩu (GET)
        /// </summary>
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login");

            try
            {
                int customerId = int.Parse(userData.CustomerId!);
                var customer = await PartnerDataService.GetCustomerAsync(customerId);

                if (customer == null)
                {
                    ModelState.AddModelError("", "Không tìm thấy thông tin khách hàng.");
                    return View(model);
                }

                string hashedCurrentPwd = CryptHelper.HashMD5(model.CurrentPassword);
                var verifyAccount = await SecurityDataService.AuthorizeCustomerAsync(customer.Email, hashedCurrentPwd);

                if (verifyAccount == null)
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                    return View(model);
                }

                string hashedNewPwd = CryptHelper.HashMD5(model.NewPassword);
                bool success = await SecurityDataService.ChangeCustomerPasswordAsync(customer.Email, hashedNewPwd);

                if (success)
                {
                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                    return RedirectToAction("Profile");
                }

                ModelState.AddModelError("", "Đổi mật khẩu thất bại. Vui lòng thử lại sau.");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
                return View(model);
            }
        }

        /// <summary>
        /// Kiểm tra file ảnh hợp lệ — trả về null nếu OK, chuỗi thông báo lỗi nếu không hợp lệ
        /// </summary>
        private static string? ValidateAvatarFile(IFormFile file)
        {
            if (file.Length > MaxAvatarSize)
                return "Ảnh đại diện không được vượt quá 2MB.";

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return "Chỉ chấp nhận file ảnh định dạng .jpg, .jpeg, hoặc .png.";

            return null;
        }

        /// <summary>Lưu file ảnh đại diện vào wwwroot và trả về tên file mới.</summary>
        private async Task<string> SaveAvatarFileAsync(IFormFile file, int customerId)
        {
            var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadDir = Path.Combine(webRoot, AvatarFolder);

            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"customer_{customerId}_{DateTime.UtcNow.Ticks}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }

        /// <summary>Map Customer entity sang ProfileViewModel.</summary>
        private static ProfileViewModel MapToProfileViewModel(Customer customer, string? claimsPhoto)
        {
            return new ProfileViewModel
            {
                CustomerId   = customer.CustomerID,
                CustomerName = customer.CustomerName,
                Email        = customer.Email,
                ContactName  = customer.ContactName,
                Phone        = customer.Phone,
                Address      = customer.Address,
                Province     = customer.Province,
                Photo        = claimsPhoto ?? ""
            };
        }
    }
}
