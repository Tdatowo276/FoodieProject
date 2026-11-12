using System.ComponentModel.DataAnnotations;

namespace FoodieAPI.Models
{
    /// <summary>Góp ý của khách gửi cho chủ shop (form "Gửi góp ý" trên trang khách hàng).</summary>
    public class Feedback
    {
        public int Id { get; set; }

        // Người gửi đã đăng nhập thì gắn tài khoản; khách vãng lai thì NULL
        public int? UserId { get; set; }
        public User? User { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = "";

        [Required, EmailAddress, StringLength(254)]
        public string Email { get; set; } = "";

        [StringLength(15)]
        public string? Phone { get; set; }

        [Required, StringLength(30)]
        public string Topic { get; set; } = FeedbackTopics.Other;

        [Range(1, 5)]
        public int? Rating { get; set; }

        [Required, StringLength(1000)]
        public string Message { get; set; } = "";

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public static class FeedbackTopics
    {
        public const string Food = "food";
        public const string Delivery = "delivery";
        public const string Service = "service";
        public const string Website = "website";
        public const string Other = "other";

        public static string Label(string topic) => topic switch
        {
            Food => "Chất lượng món ăn",
            Delivery => "Giao hàng",
            Service => "Thái độ phục vụ",
            Website => "Trang web / ứng dụng",
            _ => "Khác"
        };
    }
}
