using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020232.DataLayers.Interfaces;
using SV22T1020232.Models.Common;
using SV22T1020232.Models.Partner;


namespace SV22T1020232.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các thao tác dữ liệu cho Nhà cung cấp trên SQL Server
    /// </summary>
    public class SupplierRepository : IGenericRepository<Supplier>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhà cung cấp dưới dạng phân trang
        /// </summary>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Supplier>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Tạo tham số cho truy vấn
                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    Offset = input.Offset,
                    PageSize = input.PageSize
                };

                // Câu lệnh SQL lấy dữ liệu phân trang và đếm tổng số dòng
                string sql = @"
                    SELECT COUNT(*) FROM Suppliers 
                    WHERE (SupplierName LIKE @SearchValue) OR (ContactName LIKE @SearchValue);

                    SELECT * FROM Suppliers 
                    WHERE (SupplierName LIKE @SearchValue) OR (ContactName LIKE @SearchValue)
                    ORDER BY SupplierName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                if (input.PageSize == 0) // Trường hợp không phân trang
                {
                    sql = @"
                        SELECT COUNT(*) FROM Suppliers 
                        WHERE (SupplierName LIKE @SearchValue) OR (ContactName LIKE @SearchValue);

                        SELECT * FROM Suppliers 
                        WHERE (SupplierName LIKE @SearchValue) OR (ContactName LIKE @SearchValue)
                        ORDER BY SupplierName;";
                }

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Supplier>()).ToList();
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy thông tin một nhà cung cấp theo ID
        /// </summary>
        public async Task<Supplier?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM Suppliers WHERE SupplierID = @SupplierID";
                return await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { SupplierID = id });
            }
        }

        /// <summary>
        /// Thêm mới một nhà cung cấp
        /// </summary>
        public async Task<int> AddAsync(Supplier data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"INSERT INTO Suppliers(SupplierName, ContactName, Province, Address, Phone, Email)
                             VALUES(@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                             SELECT CAST(SCOPE_IDENTITY() as int);";

                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// Cập nhật thông tin nhà cung cấp
        /// </summary>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"UPDATE Suppliers 
                             SET SupplierName = @SupplierName, 
                                 ContactName = @ContactName, 
                                 Province = @Province, 
                                 Address = @Address, 
                                 Phone = @Phone, 
                                 Email = @Email
                             WHERE SupplierID = @SupplierID";

                int rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Xóa một nhà cung cấp
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM Suppliers WHERE SupplierID = @SupplierID";
                int rowsAffected = await connection.ExecuteAsync(sql, new { SupplierID = id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Kiểm tra xem nhà cung cấp có đang được sử dụng ở bảng khác (vd: Products) hay không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Giả sử có bảng Products tham chiếu đến SupplierID
                string sql = "SELECT CASE WHEN EXISTS(SELECT 1 FROM Products WHERE SupplierID = @SupplierID) THEN 1 ELSE 0 END";
                return await connection.ExecuteScalarAsync<bool>(sql, new { SupplierID = id });
            }
        }
    }
}