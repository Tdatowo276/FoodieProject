using System.ComponentModel.DataAnnotations;
using FoodieAPI.Common;

namespace FoodieAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Username { get; set; } = "";      // tên hiển thị (họ và tên)

        [Required, EmailAddress, StringLength(254)]
        public string Email { get; set; } = "";

        [Required, StringLength(100)]
        public string PasswordHash { get; set; } = "";  // BCrypt hash, không bao giờ lưu mật khẩu gốc

        [Required, StringLength(20)]
        public string Role { get; set; } = Roles.Customer;

        // Admin có thể khóa tài khoản khách hàng; tài khoản bị khóa không đăng nhập được
        public bool IsLocked { get; set; }

        // Ảnh đại diện (đường dẫn /uploads/{guid}.png do ImageStorage sinh ra)
        [StringLength(255)]
        public string? AvatarUrl { get; set; }

        // Địa chỉ giao hàng mặc định (chọn sẵn khi thanh toán). Cả 3 cùng có hoặc cùng trống.
        [StringLength(100)]
        public string? DefaultRecipientName { get; set; }

        [StringLength(15)]
        public string? DefaultPhone { get; set; }

        [StringLength(250)]
        public string? DefaultAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Order> Orders { get; set; } = [];
    }
}
