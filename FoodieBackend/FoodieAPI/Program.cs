using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
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

// 1. Terminal chọn CSDL
Console.WriteLine("==================================================");
Console.WriteLine("          CHỌN CƠ SỞ DỮ LIỆU CHO FOODIE API         ");
Console.WriteLine("==================================================");
Console.WriteLine("1. Dùng SQLite cục bộ (Tự động sinh file 'foodie.db')");
Console.WriteLine("2. Dùng SQL Server trên máy (Nhập tên Server và Database)");
Console.Write("👉 Lựa chọn của bạn (1 hoặc 2, mặc định là 1): ");

string? choice = Console.ReadLine()?.Trim();
string connectionString;
bool isSqlite = true;

if (choice == "2")
{
    isSqlite = false;
    // Console.WriteLine("\n--- CẤU HÌNH SQL SERVER ---");
    // Console.Write("Nhập tên Server (ví dụ: localhost, .\\SQLEXPRESS): ");
    // string server = Console.ReadLine()?.Trim() ?? ".\\SQLEXPRESS";
    // server = server.Replace("\\\\", "\\");
    string server = ".\\SQLEXPRESS";

    // Console.Write("Nhập tên Database (ví dụ: FoodieDB): ");
    // string dbName = Console.ReadLine()?.Trim() ?? "FoodieDB";
    string dbName = "FoodieDB";

    connectionString = $"Server={server};Database={dbName};Trusted_Connection=True;TrustServerCertificate=True;";
    Console.WriteLine($"\n[INFO] Đã cấu hình kết nối tới SQL Server: {dbName} trên {server}");
}
else
{
    isSqlite = true;
    connectionString = "Data Source=foodie.db";
    Console.WriteLine("\n[INFO] Đang sử dụng cơ sở dữ liệu SQLite cục bộ ('foodie.db')...");
}
Console.WriteLine("==================================================\n");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (isSqlite) options.UseSqlite(connectionString);
    else options.UseSqlServer(connectionString);
});

// 2. JWT
var jwtKey = config["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}

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