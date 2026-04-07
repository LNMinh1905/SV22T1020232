using SV22T1020232.DataLayers.Interfaces;
using SV22T1020232.DataLayers.SQLServer;
using SV22T1020232.Models.Common;
using SV22T1020232.Models.Partner;
using SV22T1020232.BusinessLayers;


namespace SV22T1020232.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến đối tác của hệ thống
    /// Bao gồm: Customer, Supplier, Shipper
    /// </summary>
    public static class PartnerDataService
    {
        /// <summary>
        /// Khai báo các repository
        /// </summary>
        private static readonly ICustomerRepository customerDB;
        private static readonly IGenericRepository<Supplier> supplierDB;
        private static readonly IGenericRepository<Shipper> shipperDB;

        /// <summary>
        /// Constructor tĩnh
        /// </summary>
        static PartnerDataService()
        {
            customerDB = new CustomerRepository(Configuration.ConnectionString);
            supplierDB = new SupplierRepository(Configuration.ConnectionString);
            shipperDB = new ShipperRepository(Configuration.ConnectionString);
        }

        #region Supplier

        /// <summary>
        /// Tìm kiếm và trả về danh sách các nhà cung cấp dưới dạng kết quả phân trang
        /// </summary>
        public static Task<PagedResult<Supplier>> ListSupplierAsync(PaginationSearchInput input)
            => supplierDB.ListAsync(input);

        /// <summary>
        /// Lấy thông tin 1 nhà cung cấp theo mã
        /// </summary>
        public static Task<Supplier?> GetSupplierAsync(int supplierID)
            => supplierDB.GetAsync(supplierID);

        /// <summary>
        /// Bổ sung nhà cung cấp mới, trả về mã nhà cung cấp
        /// </summary>
        public static async Task<int> AddSupplierAsync(Supplier supplier)
        {
            if (supplier is null)
                throw new ArgumentNullException(nameof(supplier));

            supplier.SupplierName = supplier.SupplierName?.Trim() ?? string.Empty;
            supplier.ContactName = supplier.ContactName?.Trim() ?? string.Empty;
            supplier.Address = supplier.Address?.Trim() ?? string.Empty;
            supplier.Phone = supplier.Phone?.Trim() ?? string.Empty;
            supplier.Email = supplier.Email?.Trim() ?? string.Empty;
            supplier.Province = supplier.Province?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(supplier.SupplierName))
                throw new ArgumentException("Tên nhà cung cấp không được để trống.", nameof(supplier));

            return await supplierDB.AddAsync(supplier);
        }

        /// <summary>
        /// Cập nhật thông tin 1 nhà cung cấp
        /// </summary>
        public static async Task<bool> UpdateSupplierAsync(Supplier supplier)
        {
            if (supplier is null)
                throw new ArgumentNullException(nameof(supplier));

            supplier.SupplierName = supplier.SupplierName?.Trim() ?? string.Empty;
            supplier.ContactName = supplier.ContactName?.Trim() ?? string.Empty;
            supplier.Address = supplier.Address?.Trim() ?? string.Empty;
            supplier.Phone = supplier.Phone?.Trim() ?? string.Empty;
            supplier.Email = supplier.Email?.Trim() ?? string.Empty;
            supplier.Province = supplier.Province?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(supplier.SupplierName))
                throw new ArgumentException("Tên nhà cung cấp không được để trống.", nameof(supplier));

            return await supplierDB.UpdateAsync(supplier);
        }

        /// <summary>
        /// Xóa 1 nhà cung cấp theo mã (trả về false nếu đang được sử dụng)
        /// </summary>
        public static async Task<bool> DeleteSupplierAsync(int supplierID)
        {
            if (await supplierDB.IsUsedAsync(supplierID))
                return false;
            return await supplierDB.DeleteAsync(supplierID);
        }

        /// <summary>
        /// Kiểm tra nhà cung cấp có dữ liệu liên quan hay không
        /// </summary>
        public static Task<bool> IsUsedSupplierAsync(int supplierID)
            => supplierDB.IsUsedAsync(supplierID);

        #endregion

        #region Customer

        /// <summary>
        /// Tìm kiếm và trả về danh sách khách hàng dưới dạng phân trang
        /// </summary>
        public static Task<PagedResult<Customer>> ListCustomerAsync(PaginationSearchInput input)
            => customerDB.ListAsync(input);

        /// <summary>
        /// Lấy thông tin 1 khách hàng
        /// </summary>
        public static Task<Customer?> GetCustomerAsync(int customerID)
            => customerDB.GetAsync(customerID);

        /// <summary>
        /// Bổ sung khách hàng mới, trả về mã khách hàng
        /// </summary>
        public static async Task<int> AddCustomerAsync(Customer customer)
        {
            if (customer is null)
                throw new ArgumentNullException(nameof(customer));

            customer.CustomerName = customer.CustomerName?.Trim() ?? string.Empty;
            customer.ContactName = customer.ContactName?.Trim() ?? string.Empty;
            customer.Address = customer.Address?.Trim() ?? string.Empty;
            customer.Phone = customer.Phone?.Trim() ?? string.Empty;
            customer.Email = customer.Email?.Trim() ?? string.Empty;
            customer.Province = customer.Province?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(customer.CustomerName))
                throw new ArgumentException("Tên khách hàng không được để trống.", nameof(customer));

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                var ok = await customerDB.ValidateEmailAsync(customer.Email, 0);
                if (!ok)
                    throw new ArgumentException("Email khách hàng đã được sử dụng.", nameof(customer));
            }

            return await customerDB.AddAsync(customer);
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        public static async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            if (customer is null)
                throw new ArgumentNullException(nameof(customer));

            customer.CustomerName = customer.CustomerName?.Trim() ?? string.Empty;
            customer.ContactName = customer.ContactName?.Trim() ?? string.Empty;
            customer.Address = customer.Address?.Trim() ?? string.Empty;
            customer.Phone = customer.Phone?.Trim() ?? string.Empty;
            customer.Email = customer.Email?.Trim() ?? string.Empty;
            customer.Province = customer.Province?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(customer.CustomerName))
                throw new ArgumentException("Tên khách hàng không được để trống.", nameof(customer));

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                var ok = await customerDB.ValidateEmailAsync(customer.Email, customer.CustomerID);
                if (!ok)
                    throw new ArgumentException("Email khách hàng đã được sử dụng.", nameof(customer));
            }

            return await customerDB.UpdateAsync(customer);
        }

        /// <summary>
        /// Xóa khách hàng theo mã (trả về false nếu đang được dùng trong đơn hàng)
        /// </summary>
        public static async Task<bool> DeleteCustomerAsync(int customerID)
        {
            if (await customerDB.IsUsedAsync(customerID))
                return false;
            return await customerDB.DeleteAsync(customerID);
        }

        /// <summary>
        /// Kiểm tra khách hàng có dữ liệu liên quan hay không
        /// </summary>
        public static Task<bool> IsUsedCustomerAsync(int customerID)
            => customerDB.IsUsedAsync(customerID);

        /// <summary>
        /// Kiểm tra email khách hàng hợp lệ (không trùng)
        /// </summary>
        public static Task<bool> ValidateCustomerEmailAsync(string email, int customerID = 0)
            => customerDB.ValidateEmailAsync(email, customerID);

        #endregion

        #region Shipper

        public static Task<PagedResult<Shipper>> ListShippersAsync(PaginationSearchInput input)
            => shipperDB.ListAsync(input);

        public static Task<Shipper?> GetShipperAsync(int shipperID)
            => shipperDB.GetAsync(shipperID);

        public static Task<int> AddShipperAsync(Shipper shipper)
            => shipperDB.AddAsync(shipper);

        public static Task<bool> UpdateShipperAsync(Shipper shipper)
            => shipperDB.UpdateAsync(shipper);

        public static async Task<bool> DeleteShipperAsync(int shipperID)
        {
            if (await shipperDB.IsUsedAsync(shipperID))
                return false;
            return await shipperDB.DeleteAsync(shipperID);
        }

        public static Task<bool> IsUsedShipperAsync(int shipperID)
            => shipperDB.IsUsedAsync(shipperID);

        #endregion
    }
}