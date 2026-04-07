using SV22T1020232.DataLayers.Interfaces;
using SV22T1020232.DataLayers.SQLServer;
using SV22T1020232.Models.DataDictionary;
using SV22T1020232.BusinessLayers;


namespace SV22T1020232.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến từ điển dữ liệu của ứng dụng
    /// </summary>
    public static class DictionaryDataService
    {
        private static readonly IDataDictionaryRepository<Province> provinceDB;

        /// <summary>
        /// Constructor tĩnh để khởi tạo các repository
        /// </summary>
        static DictionaryDataService()
        {
            provinceDB = new ProvinceRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Lấy danh sách toàn bộ tỉnh thành
        /// </summary>
        /// <returns></returns>
        public static async Task<List<Province>> ListProvincesAsync()
        {
            return await provinceDB.ListAsync();
        }
    }
}