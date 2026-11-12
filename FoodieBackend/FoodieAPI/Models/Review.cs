using System.ComponentModel.DataAnnotations;

namespace FoodieAPI.Models
{
    /// <summary>
    /// Đánh giá món ăn. Chỉ khách đã mua món (có đơn HOÀN THÀNH chứa món) mới được đánh giá;
    /// mỗi khách 1 đánh giá cho mỗi món (sửa được).
    /// </summary>
    public class Review
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
