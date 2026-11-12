using System.ComponentModel.DataAnnotations;

namespace FoodieAPI.DTOs
{
    public record ReviewRequest(
        [property: Required(ErrorMessage = "Vui lòng chọn số sao.")]
        [property: Range(1, 5, ErrorMessage = "Đánh giá từ 1 đến 5 sao.")]
        int? Rating,

        [property: StringLength(500, ErrorMessage = "Nhận xét tối đa 500 ký tự.")]
        string? Comment
    );

    public record ReviewDto(
        int Id,
        int ProductId,
        string ProductName,
        string UserName,
        string? UserAvatarUrl,
        int Rating,
        string? Comment,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );

    /// <summary>Tóm tắt đánh giá của một món: điểm trung bình, số lượt, số lượt theo từng mức 1-5 sao.</summary>
    public record RatingSummary(double Average, int Count, int[] Stars);

    public record ProductReviewsDto(RatingSummary Summary, Paged<ReviewDto> Reviews);

    /// <summary>Trạng thái đánh giá của người đang đăng nhập với một món.</summary>
    public record MyReviewDto(bool CanReview, string? Reason, ReviewDto? Review);

    /// <summary>Lọc đánh giá (admin).</summary>
    public record ReviewQuery(int? Rating = null, string? Keyword = null, int? Page = null, int? PageSize = null);

    /// <summary>Món bán chạy (chỉ tính đơn đã hoàn thành).</summary>
    public record TopProductDto(int ProductId, string Name, string? ImageUrl, int Quantity, long Revenue);
}
