using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020232.Models.Common;
using SV22T1020232.Models.Partner;
using SV22T1020232.DataLayers.Interfaces;
using System.Data;

namespace SV22T1020232.DataLayers.SQLServer
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Customer>()
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

                string sql = @"
                    SELECT COUNT(*) FROM Customers 
                    WHERE (CustomerName LIKE @SearchValue) OR (ContactName LIKE @SearchValue) OR (Email LIKE @SearchValue);

                    SELECT * FROM Customers 
                    WHERE (CustomerName LIKE @SearchValue) OR (ContactName LIKE @SearchValue) OR (Email LIKE @SearchValue)
                    ORDER BY CustomerName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                if (input.PageSize == 0)
                {
                    sql = @"
                        SELECT COUNT(*) FROM Customers 
                        WHERE (CustomerName LIKE @SearchValue) OR (ContactName LIKE @SearchValue) OR (Email LIKE @SearchValue);

                        SELECT * FROM Customers 
                        WHERE (CustomerName LIKE @SearchValue) OR (ContactName LIKE @SearchValue) OR (Email LIKE @SearchValue)
                        ORDER BY CustomerName;";
                }

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Customer>()).ToList();
                }
            }

            return result;
        }

        public async Task<Customer?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM Customers WHERE CustomerID = @CustomerID";
                return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { CustomerID = id });
            }
        }

        /// <summary>
        /// FIX: Đã thêm cột Password vào lệnh INSERT
        /// </summary>
        public async Task<int> AddAsync(Customer data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Bổ sung Password vào đây
                string sql = @"INSERT INTO Customers(CustomerName, ContactName, Province, Address, Phone, Email, Password, IsLocked)
                             VALUES(@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @Password, @IsLocked);
                             SELECT CAST(SCOPE_IDENTITY() as int);";

                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// FIX: Đã thêm cột Password vào lệnh UPDATE (phòng trường hợp đổi mật khẩu ở Profile)
        /// </summary>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"UPDATE Customers 
                             SET CustomerName = @CustomerName, 
                                 ContactName = @ContactName, 
                                 Province = @Province, 
                                 Address = @Address, 
                                 Phone = @Phone, 
                                 Email = @Email,
                                 Password = @Password,
                                 IsLocked = @IsLocked
                             WHERE CustomerID = @CustomerID";

                int rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM Customers WHERE CustomerID = @CustomerID";
                int rowsAffected = await connection.ExecuteAsync(sql, new { CustomerID = id });
                return rowsAffected > 0;
            }
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT CASE WHEN EXISTS(SELECT 1 FROM Orders WHERE CustomerID = @CustomerID) THEN 1 ELSE 0 END";
                return await connection.ExecuteScalarAsync<bool>(sql, new { CustomerID = id });
            }
        }

        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT COUNT(*) FROM Customers WHERE Email = @Email AND CustomerID <> @CustomerID";
                int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, CustomerID = id });
                return count == 0;
            }
        }

        /// <summary>
        /// Bổ sung phương thức lấy theo Email cho Login
        /// </summary>
        public async Task<Customer?> GetByEmailAsync(string email)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM Customers WHERE Email = @Email";
                return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { Email = email });
            }
        }
    }
}