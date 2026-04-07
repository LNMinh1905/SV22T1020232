using Microsoft.AspNetCore.Authentication.Cookies;
using SV22T1020232.Shop;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ==========================================================
// 1. CẤU HÌNH CÁC DỊCH VỤ (SERVICES)
// ==========================================================

// Đăng ký HttpContextAccessor để có thể truy cập Session/User ở mọi nơi
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ShoppingCartService>();

// Cấu hình MVC
builder.Services.AddControllersWithViews()
    .AddMvcOptions(option =>
    {
        option.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

// --- Cấu hình Cookie Authentication ---
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SV22T1020232.Shop.Auth";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true; // Đảm bảo cookie auth luôn được gửi
    });

// --- Cấu hình Session (Quan trọng cho Giỏ hàng) ---
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Bắt buộc để giỏ hàng không bị mất khi dùng trình duyệt ẩn danh
    options.Cookie.Name = "SV22T1020232.Session";
});

// ==========================================================
// 2. XÂY DỰNG APP VÀ KHỞI TẠO HỆ THỐNG
// ==========================================================
var app = builder.Build();

// Lấy chuỗi kết nối và khởi tạo Business Layer
// Lưu ý: Phải đặt TRƯỚC các Middleware để đảm bảo DB sẵn sàng
string connectionString = app.Configuration.GetConnectionString("LiteCommerceDB")
    ?? throw new InvalidOperationException("ConnectionString 'LiteCommerceDB' không tìm thấy.");

SV22T1020232.BusinessLayers.Configuration.Initiallize(connectionString);


var cultureInfo = new CultureInfo("vi-VN");
cultureInfo.NumberFormat.CurrencySymbol = "đ"; // Đảm bảo ký hiệu tiền tệ chuẩn
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// ==========================================================
// 3. CẤU HÌNH HTTP REQUEST PIPELINE (MIDDLEWARE)
// ==========================================================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();        // 1. Phải có Session trước để lưu giỏ hàng/thông tin tạm
app.UseAuthentication(); // 2. Sau đó mới xác định "Anh là ai?"
app.UseAuthorization();  // 3. Cuối cùng mới check "Anh có quyền vào đây không?"

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();