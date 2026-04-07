using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SV22T1020232.BusinessLayers;
using SV22T1020232.Models.Catalog;
using SV22T1020232.Models.Common;

namespace SV22T1020232.Admin.Controllers
{
    /// <summary>
    /// Các chức năng quản lý dữ liệu liên quan đến sản phẩm
    /// </summary>
    /// 

    [Authorize]
    public class ProductController : Controller
    {
        private const string PRODUCT_SEARCH = "ProductSearchInput";

        /// <summary>
        /// Nhập đầu vào tìm kiếm và hiển thị danh sách các mặt hàng
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);
            if (input == null)
                input = new ProductSearchInput
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };

            await LoadProductFilterListsAsync(input);
            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và trả lời kết quả (AJAX)
        /// </summary>
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);
            return View(result);
        }

        private async Task LoadProductFilterListsAsync(ProductSearchInput selection)
        {
            var categorySearch = new PaginationSearchInput
            {
                Page = 1,
                PageSize = int.MaxValue,
                SearchValue = ""
            };
            var categoryResult = await CatalogDataService.ListCategoriesAsync(categorySearch);
            ViewBag.Categories = categoryResult.DataItems;
            ViewBag.CategoryList = new SelectList(categoryResult.DataItems, "CategoryID", "CategoryName", selection.CategoryID);

            var supplierSearch = new PaginationSearchInput
            {
                Page = 1,
                PageSize = int.MaxValue,
                SearchValue = ""
            };
            var supplierResult = await PartnerDataService.ListSupplierAsync(supplierSearch);
            ViewBag.Suppliers = supplierResult.DataItems;
            ViewBag.SupplierList = new SelectList(supplierResult.DataItems, "SupplierID", "SupplierName", selection.SupplierID);
        }

        /// <summary>
        /// Xem chi tiết 1 mặt hàng
        /// </summary>
        public async Task<IActionResult> Detail(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
                return RedirectToAction("Index");

            var photos = await CatalogDataService.ListProductPhotosAsync(id);
            var attributes = await CatalogDataService.ListProductAttributesAsync(id);

            ViewBag.Photos = photos;
            ViewBag.Attributes = attributes;

            return View(product);
        }

        /// <summary>
        /// Bổ sung mặt hàng mới
        /// </summary>
        public IActionResult Create()
        {
            var model = new Product
            {
                ProductID = 0,
                IsSelling = true
            };
            ViewBag.Title = "Bổ sung mặt hàng";
            return View("Edit", model);
        }

        /// <summary>
        /// Cập nhật thông tin 1 mặt hàng
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            var data = await CatalogDataService.GetProductAsync(id);
            if (data == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Cập nhật thông tin mặt hàng";

            var photos = await CatalogDataService.ListProductPhotosAsync(id);
            var attributes = await CatalogDataService.ListProductAttributesAsync(id);

            ViewBag.Photos = photos;
            ViewBag.Attributes = attributes;

            return View(data);
        }

        /// <summary>
        /// Xóa mặt hàng
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            var data = await CatalogDataService.GetProductAsync(id);
            if (data == null)
                return RedirectToAction("Index");

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Product data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.ProductName))
                    ModelState.AddModelError(nameof(data.ProductName), "Tên mặt hàng không được để trống.");
                if (data.Price < 0)
                    ModelState.AddModelError(nameof(data.Price), "Giá không hợp lệ.");

                // Đặt ảnh mặc định nếu không có
                if (string.IsNullOrWhiteSpace(data.Photo))
                    data.Photo = "nophoto.png";

                if (!ModelState.IsValid)
                {
                    ViewBag.Title = data.ProductID == 0 ? "Bổ sung mặt hàng" : "Cập nhật thông tin mặt hàng";
                    ViewBag.Photos = data.ProductID > 0
                        ? await CatalogDataService.ListProductPhotosAsync(data.ProductID)
                        : new List<ProductPhoto>();
                    ViewBag.Attributes = data.ProductID > 0
                        ? await CatalogDataService.ListProductAttributesAsync(data.ProductID)
                        : new List<ProductAttribute>();

                    return View("Edit", data);
                }

                if (data.ProductID == 0)
                {
                    data.ProductID = await CatalogDataService.AddProductAsync(data);
                    TempData["SuccessMessage"] = "Thêm mặt hàng thành công";
                    return RedirectToAction("Index");
                }
                else
                {
                    await CatalogDataService.UpdateProductAsync(data);
                    TempData["SuccessMessage"] = "Cập nhật mặt hàng thành công";
                    return RedirectToAction("Index");
                }
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận. Vui lòng thử lại sau.");
                ViewBag.Title = data.ProductID == 0 ? "Bổ sung mặt hàng" : "Cập nhật thông tin mặt hàng";
                ViewBag.Photos = data.ProductID > 0
                    ? await CatalogDataService.ListProductPhotosAsync(data.ProductID)
                    : new List<ProductPhoto>();
                ViewBag.Attributes = data.ProductID > 0
                    ? await CatalogDataService.ListProductAttributesAsync(data.ProductID)
                    : new List<ProductAttribute>();
                return View("Edit", data);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelete(int productID)
        {
            try
            {
                var deleted = await CatalogDataService.DeleteProductAsync(productID);
                if (!deleted)
                    TempData["ErrorMessage"] = "Mặt hàng đang được sử dụng nên không thể xóa.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Xóa mặt hàng không thành công. Vui lòng thử lại.";
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Hiển thị danh sách các thuộc tính của mặt hàng
        /// </summary>
        public async Task<IActionResult> ListAttributes(int id)
        {
            var attributes = await CatalogDataService.ListProductAttributesAsync(id);
            ViewBag.ProductID = id;
            return View(attributes);
        }

        /// <summary>
        /// Bổ sung thuộc tính mới cho mặt hàng
        /// </summary>
        public IActionResult CreateAttributes(int id)
        {
            var model = new ProductAttribute
            {
                AttributeID = 0,
                ProductID = id
            };
            ViewBag.ProductID = id;
            return View("EditAttribute", model);
        }

        /// <summary>
        /// Cập nhật một thuộc tính mặt hàng
        /// </summary>
        public async Task<IActionResult> EditAttributes(int id, long attributeId)
        {
            var data = await CatalogDataService.GetProductAttributeAsync(attributeId);
            if (data == null)
                return RedirectToAction("Edit", new { id });

            ViewBag.ProductID = id;
            return View("EditAttribute", data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAttribute(ProductAttribute data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.AttributeName))
                    ModelState.AddModelError(nameof(data.AttributeName), "Tên thuộc tính không được để trống.");

                if (!ModelState.IsValid)
                {
                    ViewBag.ProductID = data.ProductID;
                    return View("EditAttribute", data);
                }

                if (data.AttributeID == 0)
                {
                    data.AttributeID = await CatalogDataService.AddProductAttributeAsync(data);
                }
                else
                {
                    await CatalogDataService.UpdateProductAttributeAsync(data);
                }

                return RedirectToAction("Edit", new { id = data.ProductID });
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Lưu thuộc tính không thành công. Vui lòng thử lại.");
                ViewBag.ProductID = data.ProductID;
                return View("EditAttribute", data);
            }
        }

        public async Task<IActionResult> DeleteAttributes(int id, long attributeId)
        {
            try
            {
                await CatalogDataService.DeleteProductAttributeAsync(attributeId);
            }
            catch
            {
                TempData["ErrorMessage"] = "Xóa thuộc tính không thành công. Vui lòng thử lại.";
            }
            return RedirectToAction("Edit", new { id });
        }

        /// <summary>
        /// Hiển thị danh sách ảnh của mặt hàng
        /// </summary>
        public async Task<IActionResult> ListPhotos(int id)
        {
            var photos = await CatalogDataService.ListProductPhotosAsync(id);
            ViewBag.ProductID = id;
            return View(photos);
        }

        /// <summary>
        /// Bổ sung ảnh cho mặt hàng
        /// </summary>
        public IActionResult CreatePhoto(int id)
        {
            var model = new ProductPhoto
            {
                PhotoID = 0,
                ProductID = id,
                DisplayOrder = 1,
                IsHidden = false
            };
            ViewBag.ProductID = id;
            return View("EditPhoto", model);
        }

        /// <summary>
        /// Cập nhật ảnh mặt hàng
        /// </summary>
        public async Task<IActionResult> EditPhoto(int id, long photoId)
        {
            var data = await CatalogDataService.GetProductPhotoAsync(photoId);
            if (data == null)
                return RedirectToAction("Edit", new { id });

            ViewBag.ProductID = id;
            return View("EditPhoto", data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePhoto(ProductPhoto data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.Photo))
                    ModelState.AddModelError(nameof(data.Photo), "Tên file ảnh không được để trống.");

                if (!ModelState.IsValid)
                {
                    ViewBag.ProductID = data.ProductID;
                    return View("EditPhoto", data);
                }

                if (data.PhotoID == 0)
                {
                    data.PhotoID = await CatalogDataService.AddProductPhotoAsync(data);
                }
                else
                {
                    await CatalogDataService.UpdateProductPhotoAsync(data);
                }

                return RedirectToAction("Edit", new { id = data.ProductID });
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Lưu ảnh không thành công. Vui lòng thử lại.");
                ViewBag.ProductID = data.ProductID;
                return View("EditPhoto", data);
            }
        }

        public async Task<IActionResult> DeletePhoto(int id, long photoId)
        {
            try
            {
                await CatalogDataService.DeleteProductPhotoAsync(photoId);
            }
            catch
            {
                TempData["ErrorMessage"] = "Xóa ảnh không thành công. Vui lòng thử lại.";
            }
            return RedirectToAction("Edit", new { id });
        }
    }
}