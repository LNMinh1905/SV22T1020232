namespace SV22T1020232.Shop.Models
{
    /// <summary>
    /// Một dòng trong giỏ hàng (lưu session)
    /// </summary>
    public class CartLine
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
    }
}
