using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using FoodieAPI.Data;
using FoodieAPI.Common;
using FoodieAPI.Endpoints;
using FoodieAPI.Middlewares;
using FoodieAPI.DTOs;

Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads"));

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// 1. Cơ sở dữ liệu
// Không viết cứng chuỗi kết nối trong mã nguồn. Giá trị được lấy từ:
//   - Development: User Secrets   (dotnet user-secrets set "ConnectionStrings:Default" "...")
//   - Production:  biến môi trường ConnectionStrings__Default
var dbProvider = config["Database:Provider"] ?? "SqlServer";
var connectionString = RequireSetting(config, "ConnectionStrings:Default");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    switch (dbProvider.ToLowerInvariant())
    {
        case "sqlserver": options.UseSqlServer(connectionString); break;
        case "sqlite": options.UseSqlite(connectionString); break;
        default:
            throw new InvalidOperationException(
                $"Database:Provider '{dbProvider}' không hợp lệ. Giá trị hợp lệ: SqlServer, Sqlite.");
    }
});

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

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorizationBuilder().AddPolicy("Admin", policy => policy.RequireRole(Roles.Admin));

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 4. Seeder
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (app.Environment.IsDevelopment() || config.GetValue<bool>("Seed:Enabled"))
        await Seeder.RunAsync(db, config);
}

// 5. Middleware Pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseStatusCodePages(async context =>
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

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx => ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff"
});

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Map các Endpoint
app.MapAuth();
app.MapProducts();
app.MapOrders();
app.MapAdmin();

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