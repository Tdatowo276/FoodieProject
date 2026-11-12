using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FoodieAPI.Services
{
    /// <summary>
    /// Người dùng của request hiện tại, đọc từ claim đã được server xác thực
    /// (JWT cho API, Cookie cho khu Admin MVC; cả hai dùng chung tên claim "sub" / "role").
    /// </summary>
    public interface ICurrentUser
    {
        int? Id { get; }
        string? Email { get; }
        bool IsAuthenticated { get; }

        /// <summary>Id người dùng; ném 401 nếu chưa đăng nhập.</summary>
        int RequireId();

        /// <summary>Mô tả ngắn để ghi log, ví dụ "user#3".</summary>
        string Describe();
    }

    public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
    {
        private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

        public int? Id => int.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

        public string? Email => Principal?.FindFirstValue(JwtRegisteredClaimNames.Email);

        public int RequireId() =>
            Id ?? throw new Common.ApiException(401, "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");

        public string Describe() => Id is { } id ? $"user#{id}" : "anonymous";
    }
}
