using SV22T1020232.DataLayers.Interfaces;
using SV22T1020232.DataLayers.SQLServer;
using SV22T1020232.Models.Common;
using SV22T1020232.Models.Partner;

namespace SV22T1020232.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng nghiệp vụ liên quan đến Khách hàng, Nhà cung cấp, Người giao hàng
    /// </summary>
    public static class CommonDataService
    {
        private static readonly IGenericRepository<Customer> customerDB;
        private static readonly IGenericRepository<Supplier> supplierDB;
        private static readonly IGenericRepository<Shipper> shipperDB;

        static CommonDataService()
        {
            string connectionString = Configuration.ConnectionString;
            customerDB = new CustomerRepository(connectionString);
            supplierDB = new SupplierRepository(connectionString);
            shipperDB = new ShipperRepository(connectionString);
        }

        #region Customer (Khách hàng)

        public static Task<PagedResult<Customer>> ListCustomersAsync(PaginationSearchInput input)
            => customerDB.ListAsync(input);

        public static Task<Customer?> GetCustomerAsync(int id)
            => customerDB.GetAsync(id);

        public static async Task<int> AddCustomerAsync(Customer data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            ValidateCustomer(data);
            return await customerDB.AddAsync(data);
        }

        public static async Task<bool> UpdateCustomerAsync(Customer data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            ValidateCustomer(data);
            return await customerDB.UpdateAsync(data);
        }

        public static async Task<bool> DeleteCustomerAsync(int id)
        {
            if (await customerDB.IsUsedAsync(id)) return false;
            return await customerDB.DeleteAsync(id);
        }

        private static void ValidateCustomer(Customer data)
        {
            data.CustomerName = data.CustomerName?.Trim() ?? "";
            data.ContactName = data.ContactName?.Trim() ?? "";
            data.Address = data.Address?.Trim() ?? "";
            data.Phone = data.Phone?.Trim() ?? "";
            data.Email = data.Email?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(data.CustomerName)) throw new Exception("Tên khách hàng không được để trống");
            if (string.IsNullOrWhiteSpace(data.ContactName)) throw new Exception("Tên giao dịch không được để trống");
        }

        #endregion

        #region Supplier (Nhà cung cấp)

        public static Task<PagedResult<Supplier>> ListSuppliersAsync(PaginationSearchInput input)
            => supplierDB.ListAsync(input);

        public static Task<Supplier?> GetSupplierAsync(int id)
            => supplierDB.GetAsync(id);

        public static async Task<int> AddSupplierAsync(Supplier data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.SupplierName = data.SupplierName?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(data.SupplierName)) throw new Exception("Tên nhà cung cấp không được để trống");

            return await supplierDB.AddAsync(data);
        }

        public static async Task<bool> UpdateSupplierAsync(Supplier data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return await supplierDB.UpdateAsync(data);
        }

        public static async Task<bool> DeleteSupplierAsync(int id)
        {
            if (await supplierDB.IsUsedAsync(id)) return false;
            return await supplierDB.DeleteAsync(id);
        }

        #endregion

        #region Shipper (Người giao hàng)

        public static Task<PagedResult<Shipper>> ListShippersAsync(PaginationSearchInput input)
            => shipperDB.ListAsync(input);

        public static Task<Shipper?> GetShipperAsync(int id)
            => shipperDB.GetAsync(id);

        public static async Task<int> AddShipperAsync(Shipper data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.ShipperName = data.ShipperName?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(data.ShipperName)) throw new Exception("Tên người giao hàng không được để trống");

            return await shipperDB.AddAsync(data);
        }

        public static async Task<bool> UpdateShipperAsync(Shipper data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return await shipperDB.UpdateAsync(data);
        }

        public static async Task<bool> DeleteShipperAsync(int id)
        {
            if (await shipperDB.IsUsedAsync(id)) return false;
            return await shipperDB.DeleteAsync(id);
        }

        #endregion
    }
}