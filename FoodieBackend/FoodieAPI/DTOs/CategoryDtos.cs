using System.ComponentModel.DataAnnotations;

namespace FoodieAPI.DTOs
{
    public record CategoryDto(int Id, string Name, string? Description, bool IsActive, int ProductCount);

    public record CategoryRequest(
        [property: Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
        [property: StringLength(50, MinimumLength = 2, ErrorMessage = "Tên danh mục cần từ 2 đến 50 ký tự.")]
        string? Name,

        [property: StringLength(200, ErrorMessage = "Mô tả tối đa 200 ký tự.")]
        string? Description,

        bool IsActive = true
    );
}
