using System.ComponentModel.DataAnnotations;

namespace SV22T1020232.Shop.Models
{
    /// <summary>
    /// Model cho form đặt hàng
    /// </summary>
    public class CheckoutViewModel
    {
        /// <summary>
        /// Mã khách hàng
        /// </summary>
        public int CustomerId { get; set; }

        /// <summary>
        /// Tên khách hàng
        /// </summary>
        [Display(Name = "Tên khách hàng")]
        public string CustomerName { get; set; } = "";

        /// <summary>
        /// Email
        /// </summary>
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        /// <summary>
        /// Số điện thoại
        /// </summary>
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        /// <summary>
        /// Địa chỉ giao hàng
        /// </summary>
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [MaxLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string DeliveryAddress { get; set; } = "";

        /// <summary>
        /// Tỉnh/Thành phố giao hàng
        /// </summary>
        [Required(ErrorMessage = "Vui lòng nhập tỉnh/thành phố")]
        [MaxLength(100, ErrorMessage = "Tỉnh/Thành phố không được vượt quá 100 ký tự")]
        [Display(Name = "Tỉnh/Thành phố")]
        public string DeliveryProvince { get; set; } = "";

        /// <summary>
        /// Ghi chú đơn hàng
        /// </summary>
        [MaxLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }
    }
}
