namespace FoodieAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";      // tên hiển thị (họ và tên)
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Role { get; set; } = "Customer";  // Hoặc dùng Roles.Customer nếu để chung hằng số
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}