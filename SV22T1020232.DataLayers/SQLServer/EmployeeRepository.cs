using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020232.DataLayers.Interfaces;
using SV22T1020232.Models.Common;
using SV22T1020232.Models.HR;


namespace SV22T1020232.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các thao tác dữ liệu cho Nhân viên trên SQL Server
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhân viên có phân trang
        /// </summary>
        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Employee>()
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

                // Tìm kiếm theo tên, địa chỉ, điện thoại hoặc email
                string sql = @"
                    SELECT COUNT(*) FROM Employees 
                    WHERE (FullName LIKE @SearchValue) 
                       OR (Address LIKE @SearchValue) 
                       OR (Phone LIKE @SearchValue) 
                       OR (Email LIKE @SearchValue);

                    SELECT * FROM Employees 
                    WHERE (FullName LIKE @SearchValue) 
                       OR (Address LIKE @SearchValue) 
                       OR (Phone LIKE @SearchValue) 
                       OR (Email LIKE @SearchValue)
                    ORDER BY FullName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                if (input.PageSize == 0)
                {
                    sql = @"
                        SELECT COUNT(*) FROM Employees 
                        WHERE (FullName LIKE @SearchValue) OR (Address LIKE @SearchValue) OR (Phone LIKE @SearchValue) OR (Email LIKE @SearchValue);
                        
                        SELECT * FROM Employees 
                        WHERE (FullName LIKE @SearchValue) OR (Address LIKE @SearchValue) OR (Phone LIKE @SearchValue) OR (Email LIKE @SearchValue)
                        ORDER BY FullName;";
                }

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Employee>()).ToList();
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy thông tin một nhân viên theo mã ID
        /// </summary>
        public async Task<Employee?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM Employees WHERE EmployeeID = @EmployeeID";
                return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { EmployeeID = id });
            }
        }

        /// <summary>
        /// Thêm mới một nhân viên
        /// </summary>
        public async Task<int> AddAsync(Employee data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"INSERT INTO Employees(FullName, BirthDate, Address, Phone, Email, Photo, IsWorking)
                             VALUES(@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking);
                             SELECT CAST(SCOPE_IDENTITY() as int);";

                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        public async Task<bool> UpdateAsync(Employee data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"UPDATE Employees 
                             SET FullName = @FullName, 
                                 BirthDate = @BirthDate, 
                                 Address = @Address, 
                                 Phone = @Phone, 
                                 Email = @Email,
                                 Photo = @Photo,
                                 IsWorking = @IsWorking
                             WHERE EmployeeID = @EmployeeID";

                int rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Xóa nhân viên
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM Employees WHERE EmployeeID = @EmployeeID";
                int rowsAffected = await connection.ExecuteAsync(sql, new { EmployeeID = id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Kiểm tra nhân viên có liên quan đến các bảng khác (ví dụ: Orders) không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Giả sử nhân viên liên quan đến bảng đơn hàng (Orders)
                string sql = "SELECT CASE WHEN EXISTS(SELECT 1 FROM Orders WHERE EmployeeID = @EmployeeID) THEN 1 ELSE 0 END";
                return await connection.ExecuteScalarAsync<bool>(sql, new { EmployeeID = id });
            }
        }

        /// <summary>
        /// Kiểm tra email có hợp lệ (không bị trùng lặp) hay không
        /// </summary>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT COUNT(*) FROM Employees WHERE Email = @Email AND EmployeeID <> @EmployeeID";
                int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, EmployeeID = id });
                return count == 0;
            }
        }

        /// <summary>
        /// Đổi mật khẩu nhân viên
        /// </summary>
        public async Task<bool> ChangePasswordAsync(string email, string hashedPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE Employees SET Password = @Password WHERE Email = @Email";
                int rowsAffected = await connection.ExecuteAsync(sql, new { Email = email, Password = hashedPassword });
                return rowsAffected > 0;
            }
        }
    }
}