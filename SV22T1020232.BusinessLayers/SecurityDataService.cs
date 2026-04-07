using SV22T1020232.DataLayers.Interfaces;
using SV22T1020232.DataLayers.SQLServer;
using SV22T1020232.Models.Security;

namespace SV22T1020232.BusinessLayers
{
    /// <summary>
    /// Xử lý dữ liệu xác thực, tài khoản (Nhân viên & Khách hàng)
    /// </summary>
    public static class SecurityDataService
    {
        private static readonly IUserAccountRepository employeeAccountDB;
        private static readonly IUserAccountRepository customerAccountDB;

        static SecurityDataService()
        {
            // Repository dành cho Nhân viên (Bảng Employees)
            employeeAccountDB = new EmployeeAccountRepository(Configuration.ConnectionString);

            // Repository dành cho Khách hàng (Bảng Customers)
            // Lưu ý: Bạn cần đảm bảo đã tạo lớp CustomerAccountRepository trong DataLayers
            customerAccountDB = new CustomerAccountRepository(Configuration.ConnectionString);
        }

        #region Xử lý cho Nhân viên (Admin)

        /// <summary>
        /// Xác thực nhân viên đăng nhập Admin
        /// </summary>
        public static Task<UserAccount?> AuthorizeEmployeeAsync(string userName, string password)
            => employeeAccountDB.AuthenticateAsync(userName, password);

        /// <summary>
        /// Đổi mật khẩu nhân viên
        /// </summary>
        public static Task<bool> ChangeEmployeePasswordAsync(string userName, string password)
            => employeeAccountDB.ChangePassword(userName, password);

        #endregion

        #region Xử lý cho Khách hàng (Shop)

        /// <summary>
        /// Xác thực khách hàng đăng nhập trang Shop (Dùng bảng Customers)
        /// </summary>
        public static Task<UserAccount?> AuthorizeCustomerAsync(string userName, string password)
            => customerAccountDB.AuthenticateAsync(userName, password);

        /// <summary>
        /// Đổi mật khẩu khách hàng
        /// </summary>
        public static Task<bool> ChangeCustomerPasswordAsync(string userName, string password)
            => customerAccountDB.ChangePassword(userName, password);

        #endregion
    }
}