using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020232.DataLayers.Interfaces;
using SV22T1020232.Models.Security;


namespace SV22T1020232.DataLayers.SQLServer
{
    public class EmployeeAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public EmployeeAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UserAccount?> AuthenticateAsync(string userName, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Truy vấn từ bảng Employees
                // Giả sử bảng Employees có cột Password và RoleNames (hoặc gán mặc định)
                string sql = @"SELECT CAST(EmployeeID as nvarchar) AS UserId,
                                      Email AS UserName,
                                      FullName AS DisplayName,
                                      Email,
                                      Photo,
                                      N'sales,admin' AS RoleNames
                               FROM Employees
                               WHERE Email = @Email AND Password = @Password AND IsWorking = 1";

                var parameters = new { Email = userName, Password = password };
                return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, parameters);
            }
        }

        public async Task<bool> ChangePassword(string userName, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "UPDATE Employees SET Password = @Password WHERE Email = @Email";
                var rowsAffected = await connection.ExecuteAsync(sql, new { Email = userName, Password = password });
                return rowsAffected > 0;
            }
        }
    }
}