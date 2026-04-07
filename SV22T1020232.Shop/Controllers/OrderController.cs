using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020232.BusinessLayers;
using SV22T1020232.Models.Common;
using SV22T1020232.Models.Sales;
using SV22T1020232.Shop.Models;

namespace SV22T1020232.Shop.Controllers
{
    /// <summary>
    /// Quản lý đơn hàng cho khách hàng: Thanh toán, Theo dõi, Lịch sử, Hủy đơn
    /// </summary>
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ShoppingCartService _cart;

        public OrderController(ShoppingCartService cart)
        {
            _cart = cart;
        }

        /// <summary>
        /// Hiển thị trang thanh toán và thông tin giao hàng (GET)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = _cart.GetCart();
            if (cart == null || cart.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi đặt hàng.";
                return RedirectToAction("Index", "Cart");
            }

            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login", "Account");

            try
            {
                int customerId = int.Parse(userData.CustomerId!);
                var customer   = await PartnerDataService.GetCustomerAsync(customerId);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                    return RedirectToAction("Index", "Home");
                }

                var model = new CheckoutViewModel
                {
                    CustomerId       = customer.CustomerID,
                    CustomerName     = customer.CustomerName,
                    Email            = customer.Email,
                    Phone            = customer.Phone,
                    DeliveryAddress  = customer.Address ?? "",
                    DeliveryProvince = customer.Province ?? ""
                };

                ViewBag.Cart        = cart;
                ViewBag.TotalAmount = _cart.GetTotalAmount();
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("Index", "Cart");
            }
        }

        /// <summary>
        /// Xử lý đặt hàng — tạo đơn hàng từ giỏ hàng hiện tại (POST)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var cart = _cart.GetCart();

            if (!ModelState.IsValid)
            {
                ViewBag.Cart        = cart;
                ViewBag.TotalAmount = _cart.GetTotalAmount();
                return View(model);
            }

            if (cart == null || cart.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng đã trống.";
                return RedirectToAction("Index", "Cart");
            }

            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login", "Account");

            try
            {
                int customerId = int.Parse(userData.CustomerId!);

                // 1. Tạo đơn hàng
                var order = new Order
                {
                    CustomerID       = customerId,
                    OrderTime        = DateTime.Now,
                    DeliveryProvince = model.DeliveryProvince.Trim(),
                    DeliveryAddress  = model.DeliveryAddress.Trim(),
                    Status           = OrderStatusEnum.New,
                    EmployeeID       = null,
                    AcceptTime       = null,
                    ShipperID        = null,
                    ShippedTime      = null,
                    FinishedTime     = null
                };

                int orderId = await SalesDataService.AddOrderAsync(order);
                if (orderId <= 0)
                {
                    ModelState.AddModelError("", "Không thể tạo đơn hàng. Vui lòng thử lại.");
                    ViewBag.Cart        = cart;
                    ViewBag.TotalAmount = _cart.GetTotalAmount();
                    return View(model);
                }

                foreach (var item in cart)
                {
                    await SalesDataService.AddOrderDetailAsync(new OrderDetail
                    {
                        OrderID   = orderId,
                        ProductID = item.ProductId,
                        Quantity  = item.Quantity,
                        SalePrice = item.Price
                    });
                }

                _cart.ClearCart();
                TempData["SuccessMessage"] = $"🎉 Đặt hàng thành công! Mã đơn hàng của bạn là #{orderId}.";

                // 4. Redirect vào trang chi tiết đơn hàng vừa tạo
                return RedirectToAction("OrderDetail", new { id = orderId });
            }
            catch (ArgumentException argEx)
            {
                ModelState.AddModelError("", argEx.Message);
                ViewBag.Cart        = cart;
                ViewBag.TotalAmount = _cart.GetTotalAmount();
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                ViewBag.Cart        = cart;
                ViewBag.TotalAmount = _cart.GetTotalAmount();
                return View(model);
            }
        }

        /// <summary>
        /// Hiển thị lịch sử mua hàng của khách hàng, có lọc theo trạng thái đơn
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MyOrders(int page = 1, int? status = null)
        {
            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login", "Account");

            try
            {
                int customerId = int.Parse(userData.CustomerId!);
                if (page < 1) page = 1;

                // Status = 0 ≡ "Tất cả" (không lọc) — các giá trị thực: 1=New, 2=Accepted, 3=Shipping, 4=Completed, -1=Cancelled
                var input = new OrderSearchInput
                {
                    Page        = page,
                    PageSize    = 10,
                    CustomerID  = customerId,
                    Status      = status.HasValue ? (OrderStatusEnum)status.Value : 0,
                    SearchValue = "",
                    DateFrom    = null,
                    DateTo      = null
                };

                var result = await SalesDataService.ListOrdersAsync(input);

                var orderTotals = new Dictionary<int, decimal>();
                var detailTasks = result.DataItems
                    .Select(async o =>
                    {
                        var details = await SalesDataService.ListOrderDetailsAsync(o.OrderID);
                        return (o.OrderID, Total: details.Sum(d => d.TotalPrice));
                    });

                foreach (var t in await Task.WhenAll(detailTasks))
                    orderTotals[t.OrderID] = t.Total;

                ViewBag.OrderTotals   = orderTotals;
                ViewBag.CurrentPage   = page;
                ViewBag.TotalPages    = result.PageCount;
                ViewBag.CurrentStatus = status;

                return View(result.DataItems);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return View(new List<OrderViewInfo>());
            }
        }

        /// <summary>
        /// Hiển thị chi tiết một đơn hàng cụ thể của khách hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OrderDetail(int id)
        {
            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login", "Account");

            try
            {
                var order = await SalesDataService.GetOrderAsync(id);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("MyOrders");
                }

                // Bảo vệ: chỉ cho xem đơn của chính mình
                if (order.CustomerID?.ToString() != userData.CustomerId)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xem đơn hàng này.";
                    return RedirectToAction("MyOrders");
                }

                var orderDetails = await SalesDataService.ListOrderDetailsAsync(id);
                ViewBag.OrderDetails = orderDetails;
                ViewBag.TotalAmount  = orderDetails.Sum(d => d.TotalPrice);

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("MyOrders");
            }
        }

        /// <summary>
        /// Hủy đơn hàng — chỉ được khi đơn ở trạng thái Chờ duyệt (New)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login", "Account");

            try
            {
                var order = await SalesDataService.GetOrderAsync(orderId);

                if (order == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

                // Bảo vệ
                if (order.CustomerID?.ToString() != userData.CustomerId)
                    return Json(new { success = false, message = "Bạn không có quyền hủy đơn hàng này." });

                // Chỉ hủy được khi đang ở trạng thái New
                if (order.Status != OrderStatusEnum.New)
                    return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng khi đang ở trạng thái Chờ duyệt." });

                // Cập nhật trạng thái → Cancelled
                var orderToUpdate = new Order
                {
                    OrderID          = order.OrderID,
                    CustomerID       = order.CustomerID,
                    OrderTime        = order.OrderTime,
                    DeliveryProvince = order.DeliveryProvince,
                    DeliveryAddress  = order.DeliveryAddress,
                    EmployeeID       = order.EmployeeID,
                    AcceptTime       = order.AcceptTime,
                    ShipperID        = order.ShipperID,
                    ShippedTime      = order.ShippedTime,
                    FinishedTime     = order.FinishedTime,
                    Status           = OrderStatusEnum.Cancelled
                };

                bool success = await SalesDataService.UpdateOrderAsync(orderToUpdate);

                if (success)
                    return Json(new { success = true, message = "Đơn hàng đã được hủy thành công." });

                return Json(new { success = false, message = "Không thể hủy đơn hàng. Vui lòng thử lại." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }
}
