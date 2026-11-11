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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Order> Orders { get; set; } = [];
    }
}
