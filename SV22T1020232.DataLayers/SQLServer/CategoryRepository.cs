using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020232.DataLayers.Interfaces;
using SV22T1020232.Models.Catalog;
using SV22T1020232.Models.Common;


namespace SV22T1020232.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các thao tác dữ liệu cho Loại hàng (Category) trên SQL Server
    /// </summary>
    public class CategoryRepository : IGenericRepository<Category>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách loại hàng có phân trang
        /// </summary>
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Category>()
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

                // SQL lấy tổng số dòng và dữ liệu phân trang
                // Tìm kiếm theo tên loại hàng hoặc mô tả
                string sql = @"
                    SELECT COUNT(*) FROM Categories 
                    WHERE (CategoryName LIKE @SearchValue) OR (Description LIKE @SearchValue);

                    SELECT * FROM Categories 
                    WHERE (CategoryName LIKE @SearchValue) OR (Description LIKE @SearchValue)
                    ORDER BY CategoryName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                if (input.PageSize == 0)
                {
                    sql = @"
                        SELECT COUNT(*) FROM Categories 
                        WHERE (CategoryName LIKE @SearchValue) OR (Description LIKE @SearchValue);

                        SELECT * FROM Categories 
                        WHERE (CategoryName LIKE @SearchValue) OR (Description LIKE @SearchValue)
                        ORDER BY CategoryName;";
                }

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Category>()).ToList();
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy thông tin một loại hàng theo ID
        /// </summary>
        public async Task<Category?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM Categories WHERE CategoryID = @CategoryID";
                return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { CategoryID = id });
            }
        }

        /// <summary>
        /// Thêm mới một loại hàng
        /// </summary>
        public async Task<int> AddAsync(Category data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"INSERT INTO Categories(CategoryName, Description)
                             VALUES(@CategoryName, @Description);
                             SELECT CAST(SCOPE_IDENTITY() as int);";

                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// Cập nhật thông tin loại hàng
        /// </summary>
        public async Task<bool> UpdateAsync(Category data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"UPDATE Categories 
                             SET CategoryName = @CategoryName, 
                                 Description = @Description
                             WHERE CategoryID = @CategoryID";

                int rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Xóa loại hàng
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM Categories WHERE CategoryID = @CategoryID";
                int rowsAffected = await connection.ExecuteAsync(sql, new { CategoryID = id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Kiểm tra xem loại hàng có đang chứa sản phẩm nào hay không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Kiểm tra sự tồn tại trong bảng Products
                string sql = "SELECT CASE WHEN EXISTS(SELECT 1 FROM Products WHERE CategoryID = @CategoryID) THEN 1 ELSE 0 END";
                return await connection.ExecuteScalarAsync<bool>(sql, new { CategoryID = id });
            }
        }
    }
}