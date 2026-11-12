using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Common
{
    /// <summary>
    /// Hai cơ chế xác thực chạy song song:
    ///  - /api/*  : JWT Bearer trong header Authorization (React). Không dùng cookie
    ///              nên trình duyệt không tự gửi kèm => API không bị tấn công CSRF.
    ///  - còn lại : Cookie (khu Admin MVC). Form được bảo vệ bằng Anti-forgery token.
    /// Scheme "Smart" chọn cơ chế theo đường dẫn, nên cookie admin KHÔNG có hiệu lực với /api.
    /// </summary>
    public static class AuthenticationSetup
    {
        public const string SmartScheme = "Smart";
        public const string AdminPolicy = "Admin";

        public static IServiceCollection AddFoodieAuthentication(this IServiceCollection services, JwtSettings jwt)
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = SmartScheme;
                    options.DefaultChallengeScheme = SmartScheme;
                })
                .AddPolicyScheme(SmartScheme, "JWT cho /api, Cookie cho MVC", options =>
                {
                    options.ForwardDefaultSelector = ctx => IsApiRequest(ctx.Request)
                        ? JwtBearerDefaults.AuthenticationScheme
                        : CookieAuthenticationDefaults.AuthenticationScheme;
                })
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
                    options.Events = new JwtBearerEvents
                    {
                        // Token hợp lệ về chữ ký nhưng tài khoản đã bị khóa / xóa => từ chối
                        OnTokenValidated = async ctx =>
                        {
                            var auth = ctx.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                            if (!await auth.IsSessionValidAsync(ctx.Principal!))
                                ctx.Fail("Tài khoản không còn hợp lệ.");
                        }
                    };
                })
                .AddCookie(options =>
                {
                    options.Cookie.Name = "Foodie.Admin";
                    options.Cookie.HttpOnly = true;                          // JavaScript không đọc được (chống đánh cắp qua XSS)
                    // Lax: không gửi kèm POST/AJAX từ trang khác (giảm CSRF), vẫn giữ đăng nhập khi
                    // bấm link từ React sang trang Admin. Form còn được bảo vệ thêm bằng Anti-forgery token.
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS => cookie chỉ gửi qua HTTPS
                    options.ExpireTimeSpan = TimeSpan.FromHours(2);
                    options.SlidingExpiration = true;
                    options.LoginPath = "/Admin/Account/Login";
                    options.LogoutPath = "/Admin/Account/Logout";
                    options.AccessDeniedPath = "/Admin/Account/AccessDenied";
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnValidatePrincipal = async ctx =>
                        {
                            var auth = ctx.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                            if (!await auth.IsSessionValidAsync(ctx.Principal!))
                            {
                                ctx.RejectPrincipal();
                                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            }
                        }
                    };
                });

            // Vai trò đọc từ claim do server ký/mã hóa, không bao giờ từ dữ liệu client gửi lên
            services.AddAuthorizationBuilder()
                .AddPolicy(AdminPolicy, policy => policy.RequireAuthenticatedUser().RequireRole(Roles.Admin));

            return services;
        }

        public static bool IsApiRequest(HttpRequest request) => request.Path.StartsWithSegments("/api");
    }
}
