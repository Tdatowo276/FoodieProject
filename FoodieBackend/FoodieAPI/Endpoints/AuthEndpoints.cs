using FoodieAPI.Common;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Endpoints
{
    /// <summary>Web API đăng ký / đăng nhập cho React; trả về JWT.</summary>
    public static class AuthEndpoints
    {
        public static void MapAuth(this IEndpointRouteBuilder app)
        {
            // Giới hạn 10 request/phút/IP để chống dò mật khẩu
            var group = app.MapGroup("/api/auth").RequireRateLimiting("auth");

            group.MapPost("/register", async (RegisterRequest req, IAuthService auth, IJwtTokenService tokens) =>
                tokens.Issue(await auth.RegisterAsync(req))).Validate<RegisterRequest>();

            group.MapPost("/login", async (LoginRequest req, IAuthService auth, IJwtTokenService tokens) =>
                tokens.Issue(await auth.ValidateCredentialsAsync(req.Email, req.Password))).Validate<LoginRequest>();
        }
    }
}
