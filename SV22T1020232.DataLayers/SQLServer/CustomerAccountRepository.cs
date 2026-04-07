using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020232.DataLayers.Interfaces;
using SV22T1020232.Models.Security;

namespace SV22T1020232.DataLayers.SQLServer
{
    public class CustomerAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public CustomerAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UserAccount?> AuthenticateAsync(string userName, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Tối ưu SQL: 
                // 1. Chỉ lấy những cột cần thiết cho UserAccount
                // 2. Email nên map vào UserName
                string sql = @"SELECT CAST(CustomerID as nvarchar) AS UserId,
                                      Email AS UserName,
                                      CustomerName AS DisplayName,
                                      N'' AS Photo,
                                      N'customer' AS RoleNames
                               FROM Customers
                               WHERE Email = @Email 
                                 AND Password = @Password 
                                 AND IsLocked = 0";

                // Tham số truyền vào phải khớp với tên @ trong SQL
                var parameters = new { Email = userName, Password = password };

                return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, parameters);
            }
        }

        public async Task<bool> ChangePassword(string userName, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE Customers SET Password = @Password WHERE Email = @Email";
                var rowsAffected = await connection.ExecuteAsync(sql, new { Email = userName, Password = password });
                return rowsAffected > 0;
            }
        }
    }
}