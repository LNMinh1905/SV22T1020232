using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020232.DataLayers.Interfaces;
using SV22T1020232.Models.Common;
using SV22T1020232.Models.Partner;


namespace SV22T1020232.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các thao tác dữ liệu cho Người giao hàng (Shipper) trên SQL Server
    /// </summary>
    public class ShipperRepository : IGenericRepository<Shipper>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo với chuỗi kết nối CSDL
        /// </summary>
        /// <param name="connectionString"></param>
        public ShipperRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Truy vấn và tìm kiếm Shipper có phân trang
        /// </summary>
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Shipper>()
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
                    Offset = input.Offset,
                    PageSize = input.PageSize
                };

                // SQL lấy tổng số dòng và danh sách dữ liệu theo trang
                string sql = @"
                    SELECT COUNT(*) FROM Shippers 
                    WHERE (ShipperName LIKE @SearchValue) OR (Phone LIKE @SearchValue);

                    SELECT * FROM Shippers 
                    WHERE (ShipperName LIKE @SearchValue) OR (Phone LIKE @SearchValue)
                    ORDER BY ShipperName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                // Nếu PageSize = 0 thì lấy toàn bộ (không phân trang)
                if (input.PageSize == 0)
                {
                    sql = @"
                        SELECT COUNT(*) FROM Shippers 
                        WHERE (ShipperName LIKE @SearchValue) OR (Phone LIKE @SearchValue);

                        SELECT * FROM Shippers 
                        WHERE (ShipperName LIKE @SearchValue) OR (Phone LIKE @SearchValue)
                        ORDER BY ShipperName;";
                }

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Shipper>()).ToList();
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy thông tin một Shipper theo mã ID
        /// </summary>
        public async Task<Shipper?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM Shippers WHERE ShipperID = @ShipperID";
                return await connection.QueryFirstOrDefaultAsync<Shipper>(sql, new { ShipperID = id });
            }
        }

        /// <summary>
        /// Thêm mới một Shipper
        /// </summary>
        public async Task<int> AddAsync(Shipper data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"INSERT INTO Shippers(ShipperName, Phone)
                             VALUES(@ShipperName, @Phone);
                             SELECT CAST(SCOPE_IDENTITY() as int);";

                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// Cập nhật thông tin Shipper
        /// </summary>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"UPDATE Shippers 
                             SET ShipperName = @ShipperName, 
                                 Phone = @Phone
                             WHERE ShipperID = @ShipperID";

                int rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Xóa Shipper theo ID
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM Shippers WHERE ShipperID = @ShipperID";
                int rowsAffected = await connection.ExecuteAsync(sql, new { ShipperID = id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Kiểm tra Shipper có liên quan đến các đơn hàng (Orders) hay không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Kiểm tra sự tồn tại trong bảng Orders (thường là bảng có FK đến Shipper)
                string sql = "SELECT CASE WHEN EXISTS(SELECT 1 FROM Orders WHERE ShipperID = @ShipperID) THEN 1 ELSE 0 END";
                return await connection.ExecuteScalarAsync<bool>(sql, new { ShipperID = id });
            }
        }
    }
}