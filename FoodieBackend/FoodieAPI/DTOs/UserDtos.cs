namespace FoodieAPI.DTOs
{
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
