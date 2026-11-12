using System.ComponentModel.DataAnnotations;
using FoodieAPI.Common;

namespace FoodieAPI.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string Code { get; set; } = "";          // ví dụ FD482950

        // Tài khoản đã đặt đơn
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // Thông tin người nhận (có thể khác chủ tài khoản)
        [Required, StringLength(100)]
        public string CustomerName { get; set; } = "";

        [Required, StringLength(15)]
        public string Phone { get; set; } = "";

        [Required, StringLength(250)]
        public string Address { get; set; } = "";

        [StringLength(500)]
        public string? Note { get; set; }

        [Required, StringLength(20)]
        public string PaymentMethod { get; set; } = PaymentMethods.Cod;

        [Required, StringLength(20)]
        public string Status { get; set; } = OrderStatuses.Pending;

        public long Total { get; set; }                 // đã gồm phí giao hàng
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Mã đơn + tên người nhận, không dấu, viết thường, phục vụ tìm kiếm
        [StringLength(130)]
        public string SearchText { get; private set; } = "";

        public List<OrderItem> Items { get; set; } = [];

        public void UpdateSearchText() => SearchText = TextUtil.Normalize($"{Code} {CustomerName}");
    }
}
