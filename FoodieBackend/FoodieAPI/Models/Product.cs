using System.ComponentModel.DataAnnotations;
using FoodieAPI.Common;

namespace FoodieAPI.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Name { get; private set; } = "";

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        [Range(1_000, 10_000_000)]
        public long Price { get; set; }                 // VND, số nguyên

        [StringLength(300)]
        public string? Description { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        // Điểm trung bình + số lượt đánh giá, tính lại mỗi khi có đánh giá thêm / sửa / xóa
        // (lưu sẵn để danh sách món không phải tính AVG trên bảng Reviews mỗi lần tải)
        [Range(0, 5)]
        public double Rating { get; set; }

        public int ReviewCount { get; set; }

        public List<Review> Reviews { get; set; } = [];

        public bool IsFeatured { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Tên không dấu, viết thường, phục vụ tìm kiếm ("Phở bò" -> "pho bo")
        [StringLength(80)]
        public string SearchText { get; private set; } = "";

        public void SetName(string name)
        {
            Name = name.Trim();
            SearchText = TextUtil.Normalize(Name);
        }
    }
}
