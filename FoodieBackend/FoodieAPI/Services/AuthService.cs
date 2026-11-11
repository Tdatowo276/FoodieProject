using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using FoodieAPI.Common;
using FoodieAPI.Data;
using FoodieAPI.DTOs;
using FoodieAPI.Models;

namespace FoodieAPI.Services
{
    /// <summary>
    /// Đăng ký / kiểm tra đăng nhập. Dùng chung cho API (React, nhận JWT)
    /// và khu Admin MVC (nhận Cookie). Việc phát token/cookie do nơi gọi đảm nhận.
    /// </summary>
    public interface IAuthService
    {
        Task<User> RegisterAsync(RegisterRequest req);

        /// <summary>Trả về user nếu đúng email + mật khẩu và chưa bị khóa; ngược lại ném 401 / 403.</summary>
        Task<User> ValidateCredentialsAsync(string? email, string? password);

        /// <summary>Claim chuẩn dùng cho cả JWT và Cookie.</summary>
        List<Claim> BuildClaims(User user);

        /// <summary>
        /// Kiểm tra lại phiên đăng nhập ở mỗi request: tài khoản còn tồn tại, chưa bị khóa
        /// và vai trò không đổi so với lúc đăng nhập. Nhờ vậy khóa tài khoản có hiệu lực ngay.
        /// </summary>
        Task<bool> IsSessionValidAsync(ClaimsPrincipal principal);
    }

    public sealed class AuthService(AppDbContext db, ILogger<AuthService> logger) : IAuthService
    {
        private const string DuplicateEmail = "Email này đã có tài khoản. Hãy đăng nhập.";

        public async Task<User> RegisterAsync(RegisterRequest req)
        {
            // Định dạng đã được Data Annotations kiểm tra; kiểm tra thêm sau khi chuẩn hóa
            var name = req.Name!.Trim();
            var email = req.Email!.Trim().ToLowerInvariant();

            new RuleChecker()
                .Check(name.Length >= 2, "name", "Họ tên cần từ 2 đến 100 ký tự.")
                .Check(Patterns.Email.IsMatch(email), "email", "Email chưa đúng định dạng.")
                .ThrowIfInvalid();

            if (await db.Users.AnyAsync(u => u.Email == email))
                throw ApiException.Conflict(DuplicateEmail);

            var user = new User
            {
                Username = name,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password!),
                Role = Roles.Customer       // vai trò luôn do server gán, không nhận từ client
            };
            db.Users.Add(user);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Hai request đăng ký cùng email chạy song song: unique index chặn bản thứ hai
                throw ApiException.Conflict(DuplicateEmail);
            }

            logger.LogInformation("Đăng ký tài khoản mới {UserId}", user.Id);
            return user;
        }

        public async Task<User> ValidateCredentialsAsync(string? email, string? password)
        {
            var normalized = (email ?? "").Trim().ToLowerInvariant();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalized);

            // Luôn chạy BCrypt (kể cả khi email không tồn tại) để thời gian phản hồi như nhau,
            // tránh bị dò xem email nào đã đăng ký
            var passwordOk = BCrypt.Net.BCrypt.Verify(password ?? "", user?.PasswordHash ?? PasswordTools.DummyHash);

            if (user is null || !passwordOk)
            {
                logger.LogWarning("Đăng nhập thất bại cho email {Email}", normalized);
                throw new ApiException(401, "Email hoặc mật khẩu chưa đúng.");
            }

            // Chỉ báo bị khóa sau khi mật khẩu đúng, để không lộ thông tin tài khoản cho người lạ
            if (user.IsLocked)
            {
                logger.LogWarning("Tài khoản bị khóa cố đăng nhập: {UserId}", user.Id);
                throw new ApiException(403, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
            }

            logger.LogInformation("Người dùng {UserId} ({Role}) đăng nhập thành công", user.Id, user.Role);
            return user;
        }

        public List<Claim> BuildClaims(User user) =>
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.Username),
            new("role", user.Role)
        ];

        public async Task<bool> IsSessionValidAsync(ClaimsPrincipal principal)
        {
            if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)) return false;
            var role = principal.FindFirstValue("role");

            var valid = await db.Users.AsNoTracking()
                .AnyAsync(u => u.Id == userId && !u.IsLocked && u.Role == role);

            if (!valid) logger.LogWarning("Từ chối phiên đăng nhập của {UserId}: tài khoản bị khóa, bị xóa hoặc đổi vai trò", userId);
            return valid;
        }
    }

    /// <summary>Phát JWT cho API (React).</summary>
    public interface IJwtTokenService
    {
        AuthResult Issue(User user);
    }

    public sealed class JwtTokenService(JwtSettings jwt, IAuthService auth) : IJwtTokenService
    {
        public AuthResult Issue(User user)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(jwt.ExpiresMinutes);

            var claims = auth.BuildClaims(user);
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

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
