using Microsoft.AspNetCore.Mvc;
using SV22T1020232.BusinessLayers;

namespace SV22T1020232.Shop.Controllers
{
    /// <summary>
    /// Quản lý giỏ hàng dựa trên session
    /// </summary>
    public class CartController : Controller
    {
        private readonly ShoppingCartService _cart;

        public CartController(ShoppingCartService cart)
        {
            _cart = cart;
        }

        /// <summary>
        /// Hiển thị trang giỏ hàng với danh sách sản phẩm đã chọn
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            var items = _cart.GetCart();
            ViewBag.TotalAmount   = _cart.GetTotalAmount();
            ViewBag.TotalQuantity = _cart.GetTotalQuantity();
            return View(items);
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ hàng; nếu đã có thì cộng số lượng (POST AJAX / Mua ngay)
        /// </summary>
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, string? buyNow = null)
        {
            if (productId <= 0 || quantity <= 0)
            {
                if (!string.IsNullOrEmpty(buyNow))
                    return RedirectToAction("Index", "Product");
                return Json(new { success = false, message = "Thông tin sản phẩm không hợp lệ." });
            }

            try
            {
                var product = await CatalogDataService.GetProductAsync(productId);
                if (product == null)
                {
                    if (!string.IsNullOrEmpty(buyNow))
                        return RedirectToAction("Index", "Product");
                    return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã bị gỡ." });
                }

                _cart.AddToCart(
                    product.ProductID,
                    product.ProductName,
                    product.Photo ?? "nophoto.png",
                    product.Unit ?? "",
                    quantity,
                    product.Price
                );

                // "Mua ngay" — redirect về Checkout
                if (!string.IsNullOrEmpty(buyNow))
                    return RedirectToAction("Checkout", "Order");

                return Json(new
                {
                    success       = true,
                    message       = $"Đã thêm \"{product.ProductName}\" vào giỏ hàng!",
                    totalQuantity = _cart.GetTotalQuantity()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        

        /// <summary>
        /// Cập nhật số lượng một sản phẩm trong giỏ (POST AJAX)
        /// </summary>
        [HttpPost]
        public IActionResult UpdateCart(int productId, int quantity)
        {
            if (quantity < 1)
                return Json(new { success = false, message = "Số lượng phải ít nhất là 1." });

            try
            {
                bool updated = _cart.UpdateCart(productId, quantity);
                if (!updated)
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ." });

                var item = _cart.GetCart().FirstOrDefault(x => x.ProductId == productId);

                return Json(new
                {
                    success       = true,
                    newQuantity   = item?.Quantity ?? quantity,
                    itemTotal     = item?.Amount ?? 0,          // Thành tiền của item
                    cartTotal     = _cart.GetTotalAmount(),     // Tổng toàn giỏ
                    totalQuantity = _cart.GetTotalQuantity()    // Tổng số lượng (cho badge)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// POST AJAX: Xóa 1 item khỏi giỏ.
        /// Trả JSON: { success, message, cartTotal, totalQuantity }
        /// </summary>
        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            try
            {
                bool removed = _cart.RemoveFromCart(productId);
                if (!removed)
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ." });

                return Json(new
                {
                    success       = true,
                    message       = "Đã xóa sản phẩm khỏi giỏ hàng.",
                    cartTotal     = _cart.GetTotalAmount(),
                    totalQuantity = _cart.GetTotalQuantity()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        /// <summary>
        /// POST: Xóa toàn bộ giỏ hàng.
        /// Hỗ trợ cả AJAX (X-Requested-With) và form submit thông thường.
        /// </summary>
        [HttpPost]
        public IActionResult ClearCart()
        {
            try
            {
                _cart.ClearCart();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, message = "Đã xóa toàn bộ giỏ hàng." });

                TempData["SuccessMessage"] = "Đã xóa toàn bộ giỏ hàng.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = $"Lỗi: {ex.Message}" });

                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

    }
}
