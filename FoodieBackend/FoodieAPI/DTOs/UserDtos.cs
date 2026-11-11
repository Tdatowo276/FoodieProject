using System.ComponentModel.DataAnnotations;

namespace FoodieAPI.DTOs
{
    /// <summary>Khóa (true) hoặc mở khóa (false) tài khoản.</summary>
    public record SetLockedRequest(
        [property: Required(ErrorMessage = "Vui lòng cho biết khóa (true) hay mở khóa (false).")]
        bool? Locked
    );

    public record UserDto(
        int Id,
        string Name,
        string Email,
        string Role,
        bool IsLocked,
        DateTime CreatedAt,
        int OrderCount
    );

    /// <summary>Tiêu chí lọc danh sách người dùng (admin).</summary>
    public record UserQuery(string? Keyword = null, string? Role = null, int? Page = null, int? PageSize = null);
}
