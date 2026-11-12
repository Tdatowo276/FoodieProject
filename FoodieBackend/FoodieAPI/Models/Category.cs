using System.ComponentModel.DataAnnotations;

namespace FoodieAPI.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; } = "";

        [StringLength(200)]
        public string? Description { get; set; }

        // Danh mục bị ẩn sẽ không hiện cho khách và không được chọn khi thêm món mới
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Product> Products { get; set; } = [];
    }
}
