namespace SV22T1020232.Admin
{
    /// <summary>
    /// Biểu diễn kết quả trả về của API
    /// <summary
    public class ApiResult
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public ApiResult(int code , string message = "")
        {
            Code = code;
            Message = message;
        }
        /// <summary>
        /// Mã thông báo kết quả của API (0 = lỗi hoặc không thành công)
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Thông báo lỗi nếu có
        /// </summary>
        public string Message { get; set; } = "";

    }
}
