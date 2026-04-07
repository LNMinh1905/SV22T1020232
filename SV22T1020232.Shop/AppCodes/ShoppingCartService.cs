using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace SV22T1020232.Shop
{
    /// <summary>
    /// Model đại diện cho một sản phẩm trong giỏ hàng
    /// </summary>
    public class CartItem
    {
        /// <summary>
        /// Mã sản phẩm
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Tên sản phẩm
        /// </summary>
        public string ProductName { get; set; } = "";

        /// <summary>
        /// Ảnh sản phẩm
        /// </summary>
        public string Photo { get; set; } = "";

        /// <summary>
        /// Đơn vị tính
        /// </summary>
        public string Unit { get; set; } = "";

        /// <summary>
        /// Số lượng
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Giá bán
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Thành tiền (Quantity * Price)
        /// </summary>
        public decimal Amount => Quantity * Price;
    }

    /// <summary>
    /// Service quản lý giỏ hàng sử dụng Session
    /// Áp dụng Dependency Injection pattern
    /// </summary>
    public class ShoppingCartService
    {
        private const string CART_SESSION_KEY = "ShoppingCart";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ShoppingCartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Lấy giỏ hàng từ Session
        /// </summary>
        /// <returns>Danh sách CartItem</returns>
        public List<CartItem> GetCart()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
                return new List<CartItem>();

            var cartJson = session.GetString(CART_SESSION_KEY);
            if (string.IsNullOrEmpty(cartJson))
                return new List<CartItem>();

            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        /// <summary>
        /// Lưu giỏ hàng vào Session
        /// </summary>
        /// <param name="cart">Danh sách CartItem</param>
        private void SaveCart(List<CartItem> cart)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                var cartJson = JsonSerializer.Serialize(cart);
                session.SetString(CART_SESSION_KEY, cartJson);
            }
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ hàng
        /// Nếu sản phẩm đã có, tăng số lượng
        /// </summary>
        /// <param name="productId">Mã sản phẩm</param>
        /// <param name="productName">Tên sản phẩm</param>
        /// <param name="photo">Ảnh sản phẩm</param>
        /// <param name="unit">Đơn vị tính</param>
        /// <param name="quantity">Số lượng</param>
        /// <param name="price">Giá bán</param>
        public void AddToCart(int productId, string productName, string photo, string unit, int quantity, decimal price)
        {
            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(x => x.ProductId == productId);

            if (existingItem != null)
            {
                // Sản phẩm đã có trong giỏ, tăng số lượng
                existingItem.Quantity += quantity;
                existingItem.Price = price; // Cập nhật giá mới nhất
            }
            else
            {
                // Thêm sản phẩm mới vào giỏ
                cart.Add(new CartItem
                {
                    ProductId = productId,
                    ProductName = productName,
                    Photo = photo,
                    Unit = unit,
                    Quantity = quantity,
                    Price = price
                });
            }

            SaveCart(cart);
        }

        /// <summary>
        /// Cập nhật số lượng sản phẩm trong giỏ hàng
        /// </summary>
        /// <param name="productId">Mã sản phẩm</param>
        /// <param name="quantity">Số lượng mới</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public bool UpdateCart(int productId, int quantity)
        {
            if (quantity <= 0)
                return false;

            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                item.Quantity = quantity;
                SaveCart(cart);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Xóa sản phẩm khỏi giỏ hàng
        /// </summary>
        /// <param name="productId">Mã sản phẩm</param>
        /// <returns>True nếu xóa thành công</returns>
        public bool RemoveFromCart(int productId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        public void ClearCart()
        {
            SaveCart(new List<CartItem>());
        }

        /// <summary>
        /// Lấy tổng số lượng sản phẩm trong giỏ hàng
        /// </summary>
        /// <returns>Tổng số lượng</returns>
        public int GetTotalQuantity()
        {
            var cart = GetCart();
            return cart.Sum(x => x.Quantity);
        }

        /// <summary>
        /// Lấy tổng tiền của giỏ hàng
        /// </summary>
        /// <returns>Tổng tiền</returns>
        public decimal GetTotalAmount()
        {
            var cart = GetCart();
            return cart.Sum(x => x.Amount);
        }

        /// <summary>
        /// Kiểm tra giỏ hàng có rỗng không
        /// </summary>
        /// <returns>True nếu giỏ hàng rỗng</returns>
        public bool IsEmpty()
        {
            var cart = GetCart();
            return cart.Count == 0;
        }
    }
}
