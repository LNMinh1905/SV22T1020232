using SV22T1020232.DataLayers.Interfaces;
using SV22T1020232.DataLayers.SQLServer;
using SV22T1020232.Models.Common;
using SV22T1020232.Models.Sales;


namespace SV22T1020232.BusinessLayers
{
    /// <summary>
    /// Xử lý dữ liệu nghiệp vụ bán hàng (Order)
    /// </summary>
    public static class SalesDataService
    {
        private static readonly IOrderRepository orderDB;

        static SalesDataService()
        {
            orderDB = new OrderRepository(Configuration.ConnectionString);
        }

        public static Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input)
            => orderDB.ListAsync(input);

        public static Task<OrderViewInfo?> GetOrderAsync(int orderID)
            => orderDB.GetAsync(orderID);

        public static Task<int> AddOrderAsync(Order data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            data.DeliveryProvince = data.DeliveryProvince?.Trim() ?? string.Empty;
            data.DeliveryAddress = data.DeliveryAddress?.Trim() ?? string.Empty;

            if (data.CustomerID <= 0)
                throw new ArgumentException("Khách hàng không hợp lệ.", nameof(data));
            if (string.IsNullOrWhiteSpace(data.DeliveryAddress))
                throw new ArgumentException("Địa chỉ giao hàng không được để trống.", nameof(data));

            return orderDB.AddAsync(data);
        }

        public static Task<bool> UpdateOrderAsync(Order data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            data.DeliveryProvince = data.DeliveryProvince?.Trim() ?? string.Empty;
            data.DeliveryAddress = data.DeliveryAddress?.Trim() ?? string.Empty;

            if (data.CustomerID <= 0)
                throw new ArgumentException("Khách hàng không hợp lệ.", nameof(data));
            if (string.IsNullOrWhiteSpace(data.DeliveryAddress))
                throw new ArgumentException("Địa chỉ giao hàng không được để trống.", nameof(data));

            return orderDB.UpdateAsync(data);
        }

        public static Task<bool> DeleteOrderAsync(int orderID)
            => orderDB.DeleteAsync(orderID);

        #region Order Details

        public static Task<List<OrderDetailViewInfo>> ListOrderDetailsAsync(int orderID)
            => orderDB.ListDetailsAsync(orderID);

        public static Task<OrderDetailViewInfo?> GetOrderDetailAsync(int orderID, int productID)
            => orderDB.GetDetailAsync(orderID, productID);

        public static Task<bool> AddOrderDetailAsync(OrderDetail data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            if (data.Quantity <= 0)
                throw new ArgumentException("Số lượng phải lớn hơn 0.", nameof(data));
            if (data.SalePrice < 0)
                throw new ArgumentException("Giá bán không hợp lệ.", nameof(data));

            return orderDB.AddDetailAsync(data);
        }

        public static Task<bool> UpdateOrderDetailAsync(OrderDetail data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            if (data.Quantity <= 0)
                throw new ArgumentException("Số lượng phải lớn hơn 0.", nameof(data));
            if (data.SalePrice < 0)
                throw new ArgumentException("Giá bán không hợp lệ.", nameof(data));

            return orderDB.UpdateDetailAsync(data);
        }

        public static Task<bool> DeleteOrderDetailAsync(int orderID, int productID)
            => orderDB.DeleteDetailAsync(orderID, productID);

        public static Task<bool> DeleteOrderDetailsAsync(int orderID)
            => orderDB.DeleteDetailsAsync(orderID);

        #endregion
    }
}