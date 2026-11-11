using System.ComponentModel.DataAnnotations;

namespace FoodieAPI.DTOs
{
    public record FeedbackRequest(
        [property: Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [property: StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên cần từ 2 đến 100 ký tự.")]
        string? Name,

        [property: Required(ErrorMessage = "Vui lòng nhập email để chủ shop phản hồi.")]
        [property: EmailAddress(ErrorMessage = "Email chưa đúng định dạng.")]
        [property: StringLength(254)]
        string? Email,

        [property: RegularExpression(@"^\s*((0|\+84)\d{9})?\s*$", ErrorMessage = "Số điện thoại chưa đúng (ví dụ 0901234567).")]
        string? Phone,

        [property: Required(ErrorMessage = "Vui lòng chọn chủ đề.")]
        [property: RegularExpression("^(food|delivery|service|website|other)$", ErrorMessage = "Chủ đề không hợp lệ.")]
        string? Topic,

        [property: Range(1, 5, ErrorMessage = "Đánh giá từ 1 đến 5 sao.")]
        int? Rating,

        [property: Required(ErrorMessage = "Vui lòng nhập nội dung góp ý.")]
        [property: StringLength(1000, MinimumLength = 10, ErrorMessage = "Nội dung cần từ 10 đến 1000 ký tự.")]
        string? Message
    );

    public record FeedbackDto(
        int Id,
        string Name,
        string Email,
        string? Phone,
        string Topic,
        int? Rating,
        string Message,
        bool IsRead,
        bool FromMember,     // gửi khi đã đăng nhập
        DateTime CreatedAt
    );

    /// <summary>Lọc góp ý (admin). Unread = true: chỉ góp ý chưa đọc.</summary>
    public record FeedbackQuery(bool? Unread = null, string? Keyword = null, int? Page = null, int? PageSize = null);
}
