using System.ComponentModel.DataAnnotations;

namespace FoodieAPI.DTOs
{
    public record ProductDto(
        int Id,
        string Name,
        int CategoryId,
        string Category,        // tên danh mục, để hiển thị
        long Price,
        double Rating,
        string? Description,
        string? ImageUrl,
        bool IsFeatured,
        DateTime CreatedAt
    );

    public record ProductRequest(
        [property: Required(ErrorMessage = "Vui lòng nhập tên món.")]
        [property: StringLength(80, MinimumLength = 2, ErrorMessage = "Tên món cần từ 2 đến 80 ký tự.")]
        string? Name,

        [property: Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        [property: Range(1, int.MaxValue, ErrorMessage = "Danh mục không hợp lệ.")]
        int? CategoryId,

        [property: Required(ErrorMessage = "Vui lòng nhập giá.")]
        [property: Range(1_000, 10_000_000, ErrorMessage = "Giá phải từ 1.000đ đến 10.000.000đ.")]
        long? Price,

        [property: StringLength(300, ErrorMessage = "Mô tả tối đa 300 ký tự.")]
        string? Description,

        [property: RegularExpression(@"^/uploads/[0-9a-f]{32}\.(jpg|png)$",
            ErrorMessage = "Đường dẫn ảnh không hợp lệ. Hãy dùng ảnh đã tải lên từ hệ thống.")]
        string? ImageUrl,

        bool IsFeatured = false
    );

    public record UploadResult(string Url);
}
