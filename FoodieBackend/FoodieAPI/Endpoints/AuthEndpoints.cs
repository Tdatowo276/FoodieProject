using FoodieAPI.Common;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Endpoints
{
    /// <summary>Web API tài khoản cho React: đăng ký / đăng nhập nhận JWT.</summary>
    public static class AuthEndpoints
    {
        public static void MapAuth(this IEndpointRouteBuilder app)
        {
            var auth = app.MapGroup("/api/auth").WithTags("Tài khoản");

            // Giới hạn 10 request/phút/IP cho đăng ký + đăng nhập để chống dò mật khẩu
            auth.MapPost("/register", async (RegisterRequest req, IAuthService service, IJwtTokenService tokens) =>
                    tokens.Issue(await service.RegisterAsync(req)))
                .Validate<RegisterRequest>()
                .RequireRateLimiting("auth")
                .WithSummary("Đăng ký tài khoản khách hàng, trả về JWT")
                .WithDescription("Vai trò luôn là Customer do server gán; client không thể tự đặt vai trò.")
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status409Conflict)
                .ProducesProblem(StatusCodes.Status429TooManyRequests);

            auth.MapPost("/login", async (LoginRequest req, IAuthService service, IJwtTokenService tokens) =>
                    tokens.Issue(await service.ValidateCredentialsAsync(req.Email, req.Password)))
                .Validate<LoginRequest>()
                .RequireRateLimiting("auth")
                .WithSummary("Đăng nhập, trả về JWT")
                .WithDescription("Gửi token trong header: Authorization: Bearer {token}. Tài khoản bị khóa trả 403.")
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status429TooManyRequests);

            auth.MapGet("/me", (IAuthService service, ICurrentUser me) => service.GetProfileAsync(me.RequireId()))
                .RequireAuthorization()
                .RequiresLogin()
                .WithSummary("Thông tin tài khoản đang đăng nhập");
        }
    }
}
