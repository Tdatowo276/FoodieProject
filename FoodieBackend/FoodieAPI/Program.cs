using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.WebEncoders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using FoodieAPI.Data;
using FoodieAPI.Common;
using FoodieAPI.Endpoints;
using FoodieAPI.Middlewares;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads"));

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// 1. Cơ sở dữ liệu (SQL Server)
// Không viết cứng chuỗi kết nối trong mã nguồn. Giá trị được lấy từ:
//   - Development: User Secrets   (dotnet user-secrets set "ConnectionStrings:Default" "...")
//   - Production:  biến môi trường ConnectionStrings__Default
var connectionString = RequireSetting(config, "ConnectionStrings:Default");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

// 2. JWT
// Khóa ký token bắt buộc phải được cấu hình (User Secrets / biến môi trường Jwt__Key), tối thiểu 32 byte.
var jwtKey = RequireSetting(config, "Jwt:Key");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key phải dài tối thiểu 32 byte.");

var jwt = new JwtSettings(
    config["Jwt:Issuer"] ?? "Foodie.Api",
    config["Jwt:Audience"] ?? "Foodie.Client",
    jwtKey,
    config.GetValue("Jwt:ExpiresMinutes", 60));
builder.Services.AddSingleton(jwt);

// JWT cho /api (React) + Cookie cho khu Admin MVC. Xem Common/AuthenticationSetup.cs
builder.Services.AddFoodieAuthentication(jwt);

// MVC (khu Admin): mọi action POST/PUT/DELETE tự động yêu cầu Anti-forgery token (chống CSRF)
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";         // AJAX trong trang admin gửi token qua header này
    options.Cookie.Name = "Foodie.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Razor giữ nguyên chữ tiếng Việt thay vì mã hóa thành &#x...; (HTML nhẹ và dễ đọc hơn).
// Các ký tự nguy hiểm < > & " ' vẫn luôn được mã hóa nên không ảnh hưởng chống XSS.
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

// Session: ghi nhớ bộ lọc / trang đang xem của admin giữa các lần chuyển trang
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "Foodie.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// 3. CORS, Rate Limiter
var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173", "http://localhost:3000"];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = Limits.MaxRequestBytes);
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = Limits.MaxRequestBytes);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Tầng Service: logic nghiệp vụ dùng chung cho Web API (React) và khu Admin MVC
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IImageStorage, ImageStorage>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IStatsService, StatsService>();

// Tài liệu REST API: /swagger (xem Common/SwaggerSetup.cs)
builder.Services.AddFoodieSwagger();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "Foodie API";
        options.EnablePersistAuthorization();   // giữ token đã nhập khi tải lại trang
        options.DisplayRequestDuration();
    });
}

// 4. Seeder
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Áp dụng các EF Core migration còn thiếu (tự tạo database nếu chưa có)
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment() || config.GetValue<bool>("Seed:Enabled"))
        await Seeder.RunAsync(db, config);
}

// 5. Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();                  // trình duyệt chỉ dùng HTTPS cho site này
    app.UseHttpsRedirection();
}

// Header bảo mật cho mọi response
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";            // chặn trình duyệt đoán sai kiểu file
    headers["X-Frame-Options"] = "DENY";                      // chặn nhúng trang vào iframe (clickjacking)
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

// Xử lý lỗi toàn cục, tách theo loại client:
//  - /api/* : trả JSON ProblemDetails cho React
//  - MVC    : chuyển tới trang lỗi HTML thân thiện (ErrorController)
app.UseWhen(ctx => AuthenticationSetup.IsApiRequest(ctx.Request), api =>
{
    api.UseMiddleware<ErrorHandlingMiddleware>();
    api.UseStatusCodePages(async context =>
    {
        var response = context.HttpContext.Response;
        var title = response.StatusCode switch
        {
            401 => "Bạn cần đăng nhập để tiếp tục.",
            403 => "Bạn không có quyền thực hiện thao tác này.",
            404 => "Không tìm thấy tài nguyên yêu cầu.",
            429 => "Bạn thao tác quá nhanh. Vui lòng thử lại sau ít phút.",
            _ => "Yêu cầu không thành công."
        };
        await response.WriteAsJsonAsync(
            new ProblemDetails { Title = title, Status = response.StatusCode },
            options: null,
            contentType: "application/problem+json");
    });
});
app.UseWhen(ctx => !AuthenticationSetup.IsApiRequest(ctx.Request), mvc =>
{
    mvc.UseExceptionHandler("/Error");                   // tự ghi log lỗi chưa xử lý
    mvc.UseStatusCodePagesWithReExecute("/Error/{0}");
});

app.UseStaticFiles();

// Định dạng số / ngày trên trang Admin theo kiểu Việt Nam (1.250.000đ, 12/11/2026)
app.UseRequestLocalization("vi-VN");

app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Web API (Minimal API) cho React
app.MapAuth();
app.MapProducts();
app.MapOrders();
app.MapAdmin();

// MVC: khu Admin ( /Admin/{controller}/{action}/{id?} ) và trang lỗi chung
app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller}/{action=Index}/{id?}");
app.MapGet("/", () => Results.Redirect("/Admin")).ExcludeFromDescription();

app.Run();

// Đọc một cấu hình bắt buộc. Thiếu thì dừng khởi động với hướng dẫn cụ thể,
// thay vì chạy tiếp với giá trị mặc định không an toàn.
static string RequireSetting(IConfiguration config, string key)
{
    var value = config[key];
    if (!string.IsNullOrWhiteSpace(value)) return value;

    throw new InvalidOperationException(
        $"Thiếu cấu hình bắt buộc '{key}'.\n" +
        $"  - Môi trường Development: dotnet user-secrets set \"{key}\" \"<giá trị>\"\n" +
        $"  - Môi trường Production:  đặt biến môi trường {key.Replace(":", "__")}\n" +
        "Xem hướng dẫn trong README.MD, mục 'Cấu hình bí mật'.");
}