using System.ComponentModel.DataAnnotations;

namespace FoodieAPI.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int? ProductId { get; set; }             // NULL khi món đã bị xóa khỏi thực đơn
        public Product? Product { get; set; }

        [Required, StringLength(80)]
        public string ProductName { get; set; } = "";   // ảnh chụp tại thời điểm đặt hàng

        [Range(1, 99)]
        public int Quantity { get; set; }

        public long UnitPrice { get; set; }
    }
}
