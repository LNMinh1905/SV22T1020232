using SV22T1020232.DataLayers.Interfaces;
using SV22T1020232.DataLayers.SQLServer;
using SV22T1020232.Models.Common;
using SV22T1020232.Models.HR;


namespace SV22T1020232.BusinessLayers
{
    /// <summary>
    /// Xử lý dữ liệu nhân sự (Employee)
    /// </summary>
    public static class HRDataService
    {
        private static readonly IEmployeeRepository employeeDB;

        static HRDataService()
        {
            employeeDB = new EmployeeRepository(Configuration.ConnectionString);
        }

        public static Task<PagedResult<Employee>> ListEmployeesAsync(PaginationSearchInput input)
            => employeeDB.ListAsync(input);

        public static Task<Employee?> GetEmployeeAsync(int employeeID)
            => employeeDB.GetAsync(employeeID);

        public static async Task<int> AddEmployeeAsync(Employee data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            data.FullName = data.FullName?.Trim() ?? string.Empty;
            data.Address = data.Address?.Trim() ?? string.Empty;
            data.Phone = data.Phone?.Trim() ?? string.Empty;
            data.Email = data.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(data.FullName))
                throw new ArgumentException("Họ tên nhân viên không được để trống.", nameof(data));

            if (!string.IsNullOrWhiteSpace(data.Email))
            {
                var ok = await employeeDB.ValidateEmailAsync(data.Email, 0);
                if (!ok)
                    throw new ArgumentException("Email nhân viên đã được sử dụng.", nameof(data));
            }

            return await employeeDB.AddAsync(data);
        }

        public static async Task<bool> UpdateEmployeeAsync(Employee data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            data.FullName = data.FullName?.Trim() ?? string.Empty;
            data.Address = data.Address?.Trim() ?? string.Empty;
            data.Phone = data.Phone?.Trim() ?? string.Empty;
            data.Email = data.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(data.FullName))
                throw new ArgumentException("Họ tên nhân viên không được để trống.", nameof(data));

            if (!string.IsNullOrWhiteSpace(data.Email))
            {
                var ok = await employeeDB.ValidateEmailAsync(data.Email, data.EmployeeID);
                if (!ok)
                    throw new ArgumentException("Email nhân viên đã được sử dụng.", nameof(data));
            }

            return await employeeDB.UpdateAsync(data);
        }

        public static async Task<bool> DeleteEmployeeAsync(int employeeID)
        {
            if (await employeeDB.IsUsedAsync(employeeID))
                return false;
            return await employeeDB.DeleteAsync(employeeID);
        }

        public static Task<bool> IsUsedEmployeeAsync(int employeeID)
            => employeeDB.IsUsedAsync(employeeID);

        public static Task<bool> ValidateEmployeeEmailAsync(string email, int employeeID = 0)
            => employeeDB.ValidateEmailAsync(email, employeeID);

        /// <summary>
        /// Đổi mật khẩu nhân viên
        /// </summary>
        public static async Task<bool> ChangeEmployeePasswordAsync(string email, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email không được để trống.", nameof(email));
            if (string.IsNullOrWhiteSpace(hashedPassword))
                throw new ArgumentException("Mật khẩu không được để trống.", nameof(hashedPassword));

            return await employeeDB.ChangePasswordAsync(email, hashedPassword);
        }
    }
}