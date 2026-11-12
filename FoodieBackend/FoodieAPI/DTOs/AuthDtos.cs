using System.ComponentModel.DataAnnotations;

namespace FoodieAPI.DTOs
{
    public record JwtSettings(string Issuer, string Audience, string Key, int ExpiresMinutes);

    public record RegisterRequest(
        [property: Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [property: StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên cần từ 2 đến 100 ký tự.")]
        string? Name,

        [property: Required(ErrorMessage = "Vui lòng nhập email.")]
        [property: EmailAddress(ErrorMessage = "Email chưa đúng định dạng.")]
        [property: StringLength(254, ErrorMessage = "Email quá dài.")]
        string? Email,

        // BCrypt chỉ dùng 72 byte đầu của mật khẩu
        [property: Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [property: StringLength(72, MinimumLength = 6, ErrorMessage = "Mật khẩu cần từ 6 đến 72 ký tự.")]
        string? Password
    );

    public record LoginRequest(
        [property: Required(ErrorMessage = "Vui lòng nhập email.")]
        [property: StringLength(254)]
        string? Email,

        [property: Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [property: StringLength(72)]
        string? Password
    );

    public record AuthUser(int Id, string Name, string Email, string Role, string? AvatarUrl = null);

    public record AuthResult(string Token, DateTime ExpiresAt, AuthUser User);
}
