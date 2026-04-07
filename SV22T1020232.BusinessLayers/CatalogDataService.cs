using SV22T1020232.DataLayers.Interfaces;
using SV22T1020232.DataLayers.SQLServer;
using SV22T1020232.Models.Catalog;
using SV22T1020232.Models.Common;
using SV22T1020232.BusinessLayers;



namespace SV22T1020232.BusinessLayers
{
    /// <summary>
    /// Xử lý dữ liệu Catalog: Category, Product, Attribute, Photo
    /// </summary>
    public static class CatalogDataService
    {
        private static readonly IGenericRepository<Category> categoryDB;
        private static readonly IProductRepository productDB;

        static CatalogDataService()
        {
            categoryDB = new CategoryRepository(Configuration.ConnectionString);
            productDB = new ProductRepository(Configuration.ConnectionString);
        }

        #region Category

        public static Task<PagedResult<Category>> ListCategoriesAsync(PaginationSearchInput input)
            => categoryDB.ListAsync(input);

        public static Task<Category?> GetCategoryAsync(int categoryID)
            => categoryDB.GetAsync(categoryID);

        public static async Task<int> AddCategoryAsync(Category data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            data.CategoryName = data.CategoryName?.Trim() ?? string.Empty;
            data.Description = data.Description?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(data.CategoryName))
                throw new ArgumentException("Tên loại hàng không được để trống.", nameof(data));

            return await categoryDB.AddAsync(data);
        }

        public static async Task<bool> UpdateCategoryAsync(Category data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            data.CategoryName = data.CategoryName?.Trim() ?? string.Empty;
            data.Description = data.Description?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(data.CategoryName))
                throw new ArgumentException("Tên loại hàng không được để trống.", nameof(data));

            return await categoryDB.UpdateAsync(data);
        }

        public static async Task<bool> DeleteCategoryAsync(int categoryID)
        {
            if (await categoryDB.IsUsedAsync(categoryID))
                return false;
            return await categoryDB.DeleteAsync(categoryID);
        }

        public static Task<bool> IsUsedCategoryAsync(int categoryID)
            => categoryDB.IsUsedAsync(categoryID);

        #endregion

        #region Product

        public static Task<PagedResult<Product>> ListProductsAsync(ProductSearchInput input)
            => productDB.ListAsync(input);

        public static Task<Product?> GetProductAsync(int productID)
            => productDB.GetAsync(productID);

        public static async Task<int> AddProductAsync(Product data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            data.ProductName = data.ProductName?.Trim() ?? string.Empty;
            data.ProductDescription = data.ProductDescription?.Trim() ?? string.Empty;
            data.Unit = data.Unit?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(data.ProductName))
                throw new ArgumentException("Tên mặt hàng không được để trống.", nameof(data));
            if (data.Price < 0)
                throw new ArgumentException("Giá bán không hợp lệ.", nameof(data));

            return await productDB.AddAsync(data);
        }

        public static async Task<bool> UpdateProductAsync(Product data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            data.ProductName = data.ProductName?.Trim() ?? string.Empty;
            data.ProductDescription = data.ProductDescription?.Trim() ?? string.Empty;
            data.Unit = data.Unit?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(data.ProductName))
                throw new ArgumentException("Tên mặt hàng không được để trống.", nameof(data));
            if (data.Price < 0)
                throw new ArgumentException("Giá bán không hợp lệ.", nameof(data));

            return await productDB.UpdateAsync(data);
        }

        public static async Task<bool> DeleteProductAsync(int productID)
        {
            if (await productDB.IsUsedAsync(productID))
                return false;
            return await productDB.DeleteAsync(productID);
        }

        public static Task<bool> IsUsedProductAsync(int productID)
            => productDB.IsUsedAsync(productID);

        #endregion

        #region Product Attributes

        public static Task<List<ProductAttribute>> ListProductAttributesAsync(int productID)
            => productDB.ListAttributesAsync(productID);

        public static Task<ProductAttribute?> GetProductAttributeAsync(long attributeID)
            => productDB.GetAttributeAsync(attributeID);

        public static async Task<long> AddProductAttributeAsync(ProductAttribute data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            data.AttributeName = data.AttributeName?.Trim() ?? string.Empty;
            data.AttributeValue = data.AttributeValue?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(data.AttributeName))
                throw new ArgumentException("Tên thuộc tính không được để trống.", nameof(data));

            return await productDB.AddAttributeAsync(data);
        }

        public static async Task<bool> UpdateProductAttributeAsync(ProductAttribute data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            data.AttributeName = data.AttributeName?.Trim() ?? string.Empty;
            data.AttributeValue = data.AttributeValue?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(data.AttributeName))
                throw new ArgumentException("Tên thuộc tính không được để trống.", nameof(data));

            return await productDB.UpdateAttributeAsync(data);
        }

        public static Task<bool> DeleteProductAttributeAsync(long attributeID)
            => productDB.DeleteAttributeAsync(attributeID);

        #endregion

        #region Product Photos

        public static Task<List<ProductPhoto>> ListProductPhotosAsync(int productID)
            => productDB.ListPhotosAsync(productID);

        public static Task<ProductPhoto?> GetProductPhotoAsync(long photoID)
            => productDB.GetPhotoAsync(photoID);

        public static async Task<long> AddProductPhotoAsync(ProductPhoto data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            data.Photo = data.Photo?.Trim() ?? string.Empty;
            data.Description = data.Description?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(data.Photo))
                throw new ArgumentException("Ảnh sản phẩm không được để trống.", nameof(data));

            return await productDB.AddPhotoAsync(data);
        }

        public static async Task<bool> UpdateProductPhotoAsync(ProductPhoto data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            data.Photo = data.Photo?.Trim() ?? string.Empty;
            data.Description = data.Description?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(data.Photo))
                throw new ArgumentException("Ảnh sản phẩm không được để trống.", nameof(data));

            return await productDB.UpdatePhotoAsync(data);
        }

        public static Task<bool> DeleteProductPhotoAsync(long photoID)
            => productDB.DeletePhotoAsync(photoID);

        #endregion
    }
}