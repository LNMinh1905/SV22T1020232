using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace SV22T1020232.Shop
{
    /// <summary>
    /// Thông tin tài khoản Customer được lưu trong phiên đăng nhập (cookie)
    /// </summary>
    public class WebUserData
    {
        /// <summary>
        /// Mã khách hàng
        /// </summary>
        public string? CustomerId { get; set; }

        /// <summary>
        /// Tên khách hàng
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Ảnh đại diện
        /// </summary>
        public string? Photo { get; set; }

        /// <summary>
        /// Lấy danh sách các Claim chứa thông tin của customer
        /// </summary>
        private List<Claim> Claims
        {
            get
            {
                List<Claim> claims = new List<Claim>()
                {
                    new Claim(nameof(CustomerId), CustomerId ?? ""),
                    new Claim(nameof(CustomerName), CustomerName ?? ""),
                    new Claim(nameof(Email), Email ?? ""),
                    new Claim(nameof(Photo), Photo ?? "")
                };
                return claims;
            }
        }

        /// <summary>
        /// Tạo ClaimsPrincipal dựa trên thông tin của customer
        /// </summary>
        /// <returns>ClaimsPrincipal để sử dụng cho Cookie Authentication</returns>
        public ClaimsPrincipal CreatePrincipal()
        {
            var claimIdentity = new ClaimsIdentity(Claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimPrincipal = new ClaimsPrincipal(claimIdentity);
            return claimPrincipal;
        }
    }

    /// <summary>
    /// Extension methods cho ClaimsPrincipal để lấy thông tin customer
    /// </summary>
    public static class WebUserExtensions
    {
        /// <summary>
        /// Lấy thông tin customer từ ClaimsPrincipal
        /// </summary>
        /// <param name="principal">ClaimsPrincipal từ User</param>
        /// <returns>WebUserData hoặc null nếu chưa đăng nhập</returns>
        public static WebUserData? GetUserData(this ClaimsPrincipal principal)
        {
            try
            {
                if (principal == null || principal.Identity == null || !principal.Identity.IsAuthenticated)
                    return null;

                var userData = new WebUserData
                {
                    CustomerId = principal.FindFirstValue(nameof(WebUserData.CustomerId)),
                    CustomerName = principal.FindFirstValue(nameof(WebUserData.CustomerName)),
                    Email = principal.FindFirstValue(nameof(WebUserData.Email)),
                    Photo = principal.FindFirstValue(nameof(WebUserData.Photo))
                };

                return userData;
            }
            catch
            {
                return null;
            }
        }
    }
}
