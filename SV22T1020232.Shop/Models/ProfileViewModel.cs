using System.ComponentModel.DataAnnotations;

namespace SV22T1020232.Shop.Models
{
    /// <summary>
    /// Model cho form cập nhật thông tin cá nhân
    /// </summary>
    public class ProfileViewModel
    {
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên phải từ 2 đến 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string CustomerName { get; set; } = "";

        /// <summary>Email không cho phép sửa</summary>
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [StringLength(100)]
        [Display(Name = "Tên giao dịch")]
        public string? ContactName { get; set; }

        [RegularExpression(@"^(0|\+84)(3[2-9]|5[6|8|9]|7[0|6-9]|8[0-6|8|9]|9[0-9])[0-9]{7}$",
            ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam (ví dụ: 0901234567)")]
        [StringLength(15)]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [StringLength(500)]
        [Display(Name = "Địa chỉ chi tiết")]
        public string? Address { get; set; }

        [StringLength(100)]
        [Display(Name = "Tỉnh/Thành phố")]
        public string? Province { get; set; }

        /// <summary>Tên file ảnh hiện tại (read-only từ DB)</summary>
        [Display(Name = "Ảnh đại diện")]
        public string? Photo { get; set; }

        /// <summary>File upload mới từ form</summary>
        [Display(Name = "Ảnh đại diện mới")]
        public IFormFile? AvatarFile { get; set; }
    }
}
