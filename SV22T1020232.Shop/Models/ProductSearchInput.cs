namespace SV22T1020232.Shop.Models
{
    public class ProductSearchInput
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string SearchValue { get; set; } = ""; // Tìm theo tên
        public int CategoryID { get; set; } = 0;      // Tìm theo loại hàng
        public decimal MinPrice { get; set; } = 0;    // Giá thấp nhất
        public decimal MaxPrice { get; set; } = 0;    // Giá cao nhất
    }
}
