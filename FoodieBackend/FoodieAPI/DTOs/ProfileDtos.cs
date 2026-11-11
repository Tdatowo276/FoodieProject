using System.ComponentModel.DataAnnotations;

namespace FoodieAPI.DTOs
{
    /// <summary>Địa chỉ giao hàng mặc định.</summary>
    public record AddressDto(string RecipientName, string Phone, string Address);

    /// <summary>Hồ sơ của người dùng đang đăng nhập.</summary>
    public record ProfileDto(
        int Id,
        string Name,
        string Email,
        string Role,
        string? AvatarUrl,
        AddressDto? DefaultAddress,
        DateTime CreatedAt
    );

    public record UpdateProfileRequest(
        [property: Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [property: StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên cần từ 2 đến 100 ký tự.")]
        string? Name
    );

    public record ChangePasswordRequest(
        [property: Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        [property: StringLength(72)]
        string? CurrentPassword,

        [property: Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [property: StringLength(72, MinimumLength = 6, ErrorMessage = "Mật khẩu mới cần từ 6 đến 72 ký tự.")]
        string? NewPassword,

        [property: Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới.")]
        [property: Compare("NewPassword", ErrorMessage = "Mật khẩu nhập lại không khớp.")]
        string? ConfirmPassword
    );

    public record AddressRequest(
        [property: Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
        [property: StringLength(100, MinimumLength = 2, ErrorMessage = "Tên người nhận cần từ 2 đến 100 ký tự.")]
        string? RecipientName,

        [property: Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [property: RegularExpression(@"^\s*(0|\+84)\d{9}\s*$", ErrorMessage = "Số điện thoại chưa đúng (ví dụ 0901234567).")]
        string? Phone,

        [property: Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        [property: StringLength(250, ErrorMessage = "Địa chỉ tối đa 250 ký tự.")]
        string? Address
    );
}
