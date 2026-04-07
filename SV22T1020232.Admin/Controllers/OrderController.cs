using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SV22T1020232.Admin;
using SV22T1020232.BusinessLayers;
using SV22T1020232.Models.Catalog;
using SV22T1020232.Models.Common;
using SV22T1020232.Models.Sales;

namespace SV22T1020232.Admin.Controllers
{
    /// <summary>
    /// Các chức năng quản lý dữ liệu liên quan đến nghiệp vụ bán hàng
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Sales},{WebUserRoles.Administrator}")]
    public class OrderController : Controller
    {
        private const string PRODUCT_SEARCH = "SearchProductCart";
        private const string ORDER_SEARCH = "OrderSearchInput";

        #region Tìm kiếm đơn hàng

        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(ORDER_SEARCH);
            if (input == null)
            {
                input = new OrderSearchInput
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            }
            return View(input);
        }

        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            var result = await SalesDataService.ListOrdersAsync(input);
            ApplicationContext.SetSessionData(ORDER_SEARCH, input);
            return View(result);
        }

        #endregion

        #region Tạo đơn hàng

        public async Task<IActionResult> Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);
            if (input == null)
            {
                input = new ProductSearchInput
                {
                    Page = 1,
                    PageSize = 3
                };
            }

            var customerSearch = new PaginationSearchInput
            {
                Page = 1,
                PageSize = 0,
                SearchValue = ""
            };
            var customers = await PartnerDataService.ListCustomerAsync(customerSearch);
            var customerOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "-- Chọn khách hàng --" }
            };
            foreach (var c in customers.DataItems)
            {
                customerOptions.Add(new SelectListItem
                {
                    Value = c.CustomerID.ToString(),
                    Text = c.CustomerName
                });
            }
            ViewBag.CustomerList = customerOptions;

            return View(input);
        }

        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);
            return View(result);
        }

        public IActionResult ShowCart()
        {
            var cart = ShoppingCartService.GetShoppingCart();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddCartItem(int productID, int quantity, decimal salePrice)
        {
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));

            if (salePrice < 0)
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));

            var product = await CatalogDataService.GetProductAsync(productID);
            if (product == null)
                return Json(new ApiResult(0, "Mặt hàng không tồn tại"));
            if (!product.IsSelling)
                return Json(new ApiResult(0, "Mặt hàng đã ngừng bán"));

            var item = new OrderDetailViewInfo
            {
                ProductID = productID,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo ?? "nophoto.png",
                Quantity = quantity,
                SalePrice = salePrice
            };
            ShoppingCartService.AddItemToCart(item);

            return Json(new ApiResult(1));
        }

        public IActionResult DeleteCartItem(int productId = 0)
        {
            if (Request.Method == "POST")
            {
                ShoppingCartService.RemoveCartItem(productId);
                return Json(new ApiResult(1));
            }
            var item = ShoppingCartService.GetCartItem(productId);
            return PartialView(item);
        }

        public IActionResult ClearCart()
        {
            if (Request.Method == "POST")
            {
                ShoppingCartService.ClearCart();
                return Json(new ApiResult(1));
            }

            return PartialView();
        }

        public IActionResult EditCartItem(int id = 0, int productId = 0)
        {
            var item = ShoppingCartService.GetCartItem(productId);
            return PartialView(item);
        }

        [HttpPost]
        public IActionResult UpdateCartItem(int productId, int quantity, decimal salePrice)
        {
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));
            if (salePrice < 0)
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));

            ShoppingCartService.UpdateCartItem(productId, quantity, salePrice);
            return Json(new ApiResult(1));
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(int customerID, string province = "", string address = "")
        {
            var cart = ShoppingCartService.GetShoppingCart();
            if (cart.Count == 0)
                return Json(new ApiResult(0, "Giỏ hàng trống"));

            if (customerID <= 0)
                return Json(new ApiResult(0, "Vui lòng chọn khách hàng"));

            if (string.IsNullOrWhiteSpace(address))
                return Json(new ApiResult(0, "Địa chỉ giao hàng không được để trống."));

            try
            {
                var order = new Order
                {
                    CustomerID = customerID,
                    OrderTime = DateTime.Now,
                    DeliveryProvince = province?.Trim() ?? "",
                    DeliveryAddress = address.Trim(),
                    Status = OrderStatusEnum.New
                };

                int orderID = await SalesDataService.AddOrderAsync(order);

                foreach (var item in cart)
                {
                    item.OrderID = orderID;
                    await SalesDataService.AddOrderDetailAsync(item);
                }

                ShoppingCartService.ClearCart();
                return Json(new ApiResult(orderID));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, ex.Message));
            }
        }

        #endregion

        #region Xem và Xử lý đơn hàng

        public async Task<IActionResult> Detail(int id)
        {
            var data = await SalesDataService.GetOrderAsync(id);
            if (data == null)
                return RedirectToAction(nameof(Index));

            var details = await SalesDataService.ListOrderDetailsAsync(id);
            ViewBag.OrderDetails = details;
            ViewBag.OrderTotal = details.Sum(d => d.Quantity * d.SalePrice);
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Accept(int id)
        {
            var data = await SalesDataService.GetOrderAsync(id);
            if (data == null)
                return NotFound();
            return PartialView(data);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Accept(int id, int confirm = 0)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return Json(new ApiResult(0, "Không tìm thấy đơn hàng."));

            if (order.Status != OrderStatusEnum.New)
                return Json(new ApiResult(0, "Chỉ duyệt được đơn hàng ở trạng thái mới."));

            var empId = GetCurrentEmployeeId();
            if (empId == null)
                return Json(new ApiResult(0, "Không xác định nhân viên xử lý."));

            order.Status = OrderStatusEnum.Accepted;
            order.EmployeeID = empId;
            order.AcceptTime = DateTime.Now;

            try
            {
                var ok = await SalesDataService.UpdateOrderAsync(order);
                return Json(ok ? new ApiResult(1, "Duyệt đơn hàng thành công") : new ApiResult(0, "Cập nhật đơn hàng thất bại."));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, ex.Message));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Shipping(int id)
        {
            var data = await SalesDataService.GetOrderAsync(id);
            if (data == null)
                return NotFound();

            var shipperSearch = new PaginationSearchInput { Page = 1, PageSize = 0, SearchValue = "" };
            var shippers = await PartnerDataService.ListShippersAsync(shipperSearch);
            ViewBag.ShipperList = new SelectList(shippers.DataItems, "ShipperID", "ShipperName");
            return PartialView(data);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Shipping(int id, int shipperID)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return Json(new ApiResult(0, "Không tìm thấy đơn hàng."));

            if (order.Status != OrderStatusEnum.Accepted)
                return Json(new ApiResult(0, "Chỉ chuyển giao được đơn đã duyệt."));

            if (shipperID <= 0)
                return Json(new ApiResult(0, "Vui lòng chọn người giao hàng."));

            order.Status = OrderStatusEnum.Shipping;
            order.ShipperID = shipperID;
            order.ShippedTime = DateTime.Now;

            try
            {
                var ok = await SalesDataService.UpdateOrderAsync(order);
                return Json(ok ? new ApiResult(1, "Chuyển giao hàng thành công") : new ApiResult(0, "Cập nhật đơn hàng thất bại."));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, ex.Message));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Finish(int id)
        {
            var data = await SalesDataService.GetOrderAsync(id);
            if (data == null)
                return NotFound();
            return PartialView(data);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Finish(int id, int confirm = 0)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return Json(new ApiResult(0, "Không tìm thấy đơn hàng."));

            if (order.Status != OrderStatusEnum.Shipping)
                return Json(new ApiResult(0, "Chỉ hoàn tất được đơn đang giao."));

            order.Status = OrderStatusEnum.Completed;
            order.FinishedTime = DateTime.Now;

            try
            {
                var ok = await SalesDataService.UpdateOrderAsync(order);
                return Json(ok ? new ApiResult(1, "Hoàn tất đơn hàng thành công") : new ApiResult(0, "Cập nhật đơn hàng thất bại."));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, ex.Message));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Reject(int id)
        {
            var data = await SalesDataService.GetOrderAsync(id);
            if (data == null)
                return NotFound();
            return PartialView(data);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Reject(int id, int confirm = 0)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return Json(new ApiResult(0, "Không tìm thấy đơn hàng."));

            if (order.Status != OrderStatusEnum.New)
                return Json(new ApiResult(0, "Chỉ từ chối được đơn hàng mới."));

            order.Status = OrderStatusEnum.Rejected;

            try
            {
                var ok = await SalesDataService.UpdateOrderAsync(order);
                return Json(ok ? new ApiResult(1, "Từ chối đơn hàng thành công") : new ApiResult(0, "Cập nhật đơn hàng thất bại."));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, ex.Message));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            var data = await SalesDataService.GetOrderAsync(id);
            if (data == null)
                return NotFound();
            return PartialView(data);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Cancel(int id, int confirm = 0)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return Json(new ApiResult(0, "Không tìm thấy đơn hàng."));

            if (order.Status != OrderStatusEnum.New)
                return Json(new ApiResult(0, "Chỉ hủy được đơn hàng mới."));

            order.Status = OrderStatusEnum.Cancelled;

            try
            {
                var ok = await SalesDataService.UpdateOrderAsync(order);
                return Json(ok ? new ApiResult(1, "Hủy đơn hàng thành công") : new ApiResult(0, "Cập nhật đơn hàng thất bại."));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, ex.Message));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var data = await SalesDataService.GetOrderAsync(id);
            if (data == null)
                return NotFound();
            return PartialView(data);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Delete(int id, int confirm = 0)
        {
            try
            {
                var ok = await SalesDataService.DeleteOrderAsync(id);
                return Json(ok ? new ApiResult(1, "Xóa đơn hàng thành công") : new ApiResult(0, "Không xóa được đơn hàng."));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, ex.Message));
            }
        }

        private int? GetCurrentEmployeeId()
        {
            var user = User.GetUserData();
            if (user?.UserId != null && int.TryParse(user.UserId, out var id))
                return id;
            return null;
        }

        #endregion

        #region Sửa đơn hàng

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return RedirectToAction(nameof(Index));

            if (order.Status != OrderStatusEnum.New)
            {
                TempData["ErrorMessage"] = "Chỉ sửa được đơn hàng ở trạng thái mới.";
                return RedirectToAction("Detail", new { id });
            }

            // Load chi tiết đơn hàng vào giỏ hàng
            var orderDetails = await SalesDataService.ListOrderDetailsAsync(id);
            ShoppingCartService.ClearCart();
            foreach (var detail in orderDetails)
            {
                ShoppingCartService.AddItemToCart(detail);
            }

            // Load thông tin khách hàng
            var input = new ProductSearchInput
            {
                Page = 1,
                PageSize = 3
            };

            var customerSearch = new PaginationSearchInput
            {
                Page = 1,
                PageSize = 0,
                SearchValue = ""
            };
            var customers = await PartnerDataService.ListCustomerAsync(customerSearch);
            var customerOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "-- Chọn khách hàng --" }
            };
            foreach (var c in customers.DataItems)
            {
                customerOptions.Add(new SelectListItem
                {
                    Value = c.CustomerID.ToString(),
                    Text = c.CustomerName,
                    Selected = c.CustomerID == order.CustomerID
                });
            }
            ViewBag.CustomerList = customerOptions;
            ViewBag.EditingOrderId = id;
            ViewBag.DeliveryProvince = order.DeliveryProvince;
            ViewBag.DeliveryAddress = order.DeliveryAddress;

            return View("Create", input);
        }

        [HttpPost]
        public async Task<IActionResult> SaveEditOrder(int orderId, int customerID, string province = "", string address = "")
        {
            var order = await SalesDataService.GetOrderAsync(orderId);
            if (order == null)
                return Json(new ApiResult(0, "Không tìm thấy đơn hàng."));

            if (order.Status != OrderStatusEnum.New)
                return Json(new ApiResult(0, "Chỉ sửa được đơn hàng ở trạng thái mới."));

            var cart = ShoppingCartService.GetShoppingCart();
            if (cart.Count == 0)
                return Json(new ApiResult(0, "Giỏ hàng trống"));

            if (customerID <= 0)
                return Json(new ApiResult(0, "Vui lòng chọn khách hàng"));

            if (string.IsNullOrWhiteSpace(address))
                return Json(new ApiResult(0, "Địa chỉ giao hàng không được để trống."));

            try
            {
                // Cập nhật thông tin đơn hàng
                order.CustomerID = customerID;
                order.DeliveryProvince = province?.Trim() ?? "";
                order.DeliveryAddress = address.Trim();

                await SalesDataService.UpdateOrderAsync(order);

                // Xóa chi tiết cũ và thêm mới
                await SalesDataService.DeleteOrderDetailsAsync(orderId);
                foreach (var item in cart)
                {
                    item.OrderID = orderId;
                    await SalesDataService.AddOrderDetailAsync(item);
                }

                ShoppingCartService.ClearCart();
                TempData["SuccessMessage"] = "Sửa đơn hàng thành công";
                return Json(new ApiResult(orderId));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, ex.Message));
            }
        }

        #endregion
    }
}
