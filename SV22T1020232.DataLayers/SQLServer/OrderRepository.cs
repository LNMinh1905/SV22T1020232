using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020232.DataLayers.Interfaces;
using SV22T1020232.Models.Common;
using SV22T1020232.Models.Sales;


namespace SV22T1020232.DataLayers.SQLServer
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Order CRUD
        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            var result = new PagedResult<OrderViewInfo>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    Status = (int)input.Status,
                    DateFrom = input.DateFrom,
                    DateTo = input.DateTo,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                };

                // Xây dựng điều kiện lọc động
                string condition = @"(@Status = 0 OR Status = @Status)
                                    AND (@DateFrom IS NULL OR OrderTime >= @DateFrom)
                                    AND (@DateTo IS NULL OR OrderTime <= @DateTo)
                                    AND (@SearchValue = '' OR CustomerName LIKE @SearchValue 
                                                           OR DeliveryAddress LIKE @SearchValue
                                                           OR DeliveryProvince LIKE @SearchValue)";

                string sql = $@"
                    SELECT COUNT(*) 
                    FROM Orders AS o
                    LEFT JOIN Customers AS c ON o.CustomerID = c.CustomerID
                    WHERE {condition};

                    SELECT o.*, 
                           c.CustomerName, c.ContactName AS CustomerContactName, c.Address AS CustomerAddress, 
                           c.Phone AS CustomerPhone, c.Email AS CustomerEmail,
                           e.FullName AS EmployeeName,
                           s.ShipperName, s.Phone AS ShipperPhone
                    FROM Orders AS o
                    LEFT JOIN Customers AS c ON o.CustomerID = c.CustomerID
                    LEFT JOIN Employees AS e ON o.EmployeeID = e.EmployeeID
                    LEFT JOIN Shippers AS s ON o.ShipperID = s.ShipperID
                    WHERE {condition}
                    ORDER BY o.OrderTime DESC, o.OrderID DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<OrderViewInfo>()).ToList();
                }
            }
            return result;
        }

        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"SELECT o.*, 
                                      c.CustomerName, c.ContactName AS CustomerContactName, c.Address AS CustomerAddress, 
                                      c.Phone AS CustomerPhone, c.Email AS CustomerEmail,
                                      e.FullName AS EmployeeName,
                                      s.ShipperName, s.Phone AS ShipperPhone
                               FROM Orders AS o
                               LEFT JOIN Customers AS c ON o.CustomerID = c.CustomerID
                               LEFT JOIN Employees AS e ON o.EmployeeID = e.EmployeeID
                               LEFT JOIN Shippers AS s ON o.ShipperID = s.ShipperID
                               WHERE o.OrderID = @OrderID";
                return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { OrderID = orderID });
            }
        }

        public async Task<int> AddAsync(Order data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"INSERT INTO Orders(CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, 
                                               EmployeeID, AcceptTime, ShipperID, ShippedTime, FinishedTime, Status)
                               VALUES(@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, 
                                      @EmployeeID, @AcceptTime, @ShipperID, @ShippedTime, @FinishedTime, @Status);
                               SELECT CAST(SCOPE_IDENTITY() as int);";
                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        public async Task<bool> UpdateAsync(Order data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"UPDATE Orders 
                               SET CustomerID = @CustomerID, OrderTime = @OrderTime, 
                                   DeliveryProvince = @DeliveryProvince, DeliveryAddress = @DeliveryAddress, 
                                   EmployeeID = @EmployeeID, AcceptTime = @AcceptTime, 
                                   ShipperID = @ShipperID, ShippedTime = @ShippedTime, 
                                   FinishedTime = @FinishedTime, Status = @Status
                               WHERE OrderID = @OrderID";
                return await connection.ExecuteAsync(sql, data) > 0;
            }
        }

        public async Task<bool> DeleteAsync(int orderID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Chú ý: SQL Server cần xóa OrderDetails trước do ràng buộc FK
                string sql = @"DELETE FROM OrderDetails WHERE OrderID = @OrderID;
                               DELETE FROM Orders WHERE OrderID = @OrderID;";
                return await connection.ExecuteAsync(sql, new { OrderID = orderID }) > 0;
            }
        }
        #endregion

        #region Order Details
        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"SELECT od.*, p.ProductName, p.Unit, p.Photo
                               FROM OrderDetails AS od
                               JOIN Products AS p ON od.ProductID = p.ProductID
                               WHERE od.OrderID = @OrderID";
                return (await connection.QueryAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID })).ToList();
            }
        }

        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"SELECT od.*, p.ProductName, p.Unit, p.Photo
                               FROM OrderDetails AS od
                               JOIN Products AS p ON od.ProductID = p.ProductID
                               WHERE od.OrderID = @OrderID AND od.ProductID = @ProductID";
                return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID, ProductID = productID });
            }
        }

        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"INSERT INTO OrderDetails(OrderID, ProductID, Quantity, SalePrice)
                               VALUES(@OrderID, @ProductID, @Quantity, @SalePrice)";
                return await connection.ExecuteAsync(sql, data) > 0;
            }
        }

        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"UPDATE OrderDetails 
                               SET Quantity = @Quantity, SalePrice = @SalePrice
                               WHERE OrderID = @OrderID AND ProductID = @ProductID";
                return await connection.ExecuteAsync(sql, data) > 0;
            }
        }

        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID";
                return await connection.ExecuteAsync(sql, new { OrderID = orderID, ProductID = productID }) > 0;
            }
        }

        public async Task<bool> DeleteDetailsAsync(int orderID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM OrderDetails WHERE OrderID = @OrderID";
                return await connection.ExecuteAsync(sql, new { OrderID = orderID }) > 0;
            }
        }
        #endregion
    }
}