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
            group.MapPost("/register", Register);
            group.MapPost("/login", Login);
        }

        private static async Task<IResult> Register(RegisterRequest req, AppDbContext db, [FromServices] JwtSettings jwt)
        {
            var name = (req.Name ?? "").Trim();
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var password = req.Password ?? "";

            new Validator()
                .Check(name.Length is >= 2 and <= 100, "name", "Họ tên cần từ 2 đến 100 ký tự.")
                .Check(email.Length <= 254 && Patterns.Email.IsMatch(email), "email", "Email chưa đúng định dạng.")
                .Check(password.Length is >= 6 and <= 72, "password", "Mật khẩu cần từ 6 đến 72 ký tự.")
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

        private static async Task<IResult> Login(LoginRequest req, AppDbContext db, [FromServices] JwtSettings jwt)
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var password = req.Password ?? "";

            new Validator()
                .Check(email.Length > 0, "email", "Vui lòng nhập email.")
                .Check(password.Length > 0, "password", "Vui lòng nhập mật khẩu.")
                .ThrowIfInvalid();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            var passwordOk = BCrypt.Net.BCrypt.Verify(password, user?.PasswordHash ?? PasswordTools.DummyHash);

            if (user is null || !passwordOk)
                throw new ApiException(401, "Email hoặc mật khẩu chưa đúng.");

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