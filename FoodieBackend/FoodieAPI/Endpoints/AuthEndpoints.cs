 using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FoodieAPI.Data;
using FoodieAPI.Models;
using FoodieAPI.DTOs;
using FoodieAPI.Common;

namespace FoodieAPI.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuth(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth").RequireRateLimiting("auth");
            group.MapPost("/register", Register).Validate<RegisterRequest>();
            group.MapPost("/login", Login).Validate<LoginRequest>();
        }

        private static async Task<IResult> Register(RegisterRequest req, AppDbContext db, [FromServices] JwtSettings jwt)
        {
            // Định dạng đã được Data Annotations kiểm tra; kiểm tra thêm sau khi chuẩn hóa
            var name = req.Name!.Trim();
            var email = req.Email!.Trim().ToLowerInvariant();
            var password = req.Password!;

            new Validator()
                .Check(name.Length >= 2, "name", "Họ tên cần từ 2 đến 100 ký tự.")
                .Check(Patterns.Email.IsMatch(email), "email", "Email chưa đúng định dạng.")
                .ThrowIfInvalid();

            if (await db.Users.AnyAsync(u => u.Email == email))
                throw new ApiException(409, "Email này đã có tài khoản. Hãy đăng nhập.");

            var user = new User
            {
                Username = name,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = Roles.Customer
            };
            db.Users.Add(user);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new ApiException(409, "Email này đã có tài khoản. Hãy đăng nhập.");
            }

            return Results.Ok(Issue(user, jwt));
        }

        private static async Task<IResult> Login(
            LoginRequest req, AppDbContext db, [FromServices] JwtSettings jwt, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("FoodieAPI.Auth");
            var email = req.Email!.Trim().ToLowerInvariant();
            var password = req.Password!;

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            var passwordOk = BCrypt.Net.BCrypt.Verify(password, user?.PasswordHash ?? PasswordTools.DummyHash);

            if (user is null || !passwordOk)
            {
                logger.LogWarning("Đăng nhập thất bại cho email {Email}", email);
                throw new ApiException(401, "Email hoặc mật khẩu chưa đúng.");
            }

            // Chỉ báo bị khóa sau khi mật khẩu đúng, để không lộ thông tin tài khoản cho người lạ
            if (user.IsLocked)
            {
                logger.LogWarning("Tài khoản bị khóa cố đăng nhập: {UserId}", user.Id);
                throw new ApiException(403, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
            }

            logger.LogInformation("Người dùng {UserId} ({Role}) đăng nhập thành công", user.Id, user.Role);

            return Results.Ok(Issue(user, jwt));
        }

        private static AuthResult Issue(User user, JwtSettings jwt)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(jwt.ExpiresMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new("name", user.Username),
                new("role", user.Role)
            };

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                Issuer = jwt.Issuer,
                Audience = jwt.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    SecurityAlgorithms.HmacSha256)
            };

            var token = new JsonWebTokenHandler().CreateToken(descriptor);
            return new AuthResult(token, expiresAt, new AuthUser(user.Id, user.Username, user.Email, user.Role));
        }
    }
}