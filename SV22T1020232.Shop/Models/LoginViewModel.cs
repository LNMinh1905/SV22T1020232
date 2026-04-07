using System.ComponentModel.DataAnnotations;

namespace SV22T1020232.Shop.Models
{
    /// <summary>
    /// Model cho form đăng nhập
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = "";

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }

        /// <summary>
        /// URL để redirect sau khi đăng nhập thành công
        /// </summary>
        public string? ReturnUrl { get; set; }
    }
}
