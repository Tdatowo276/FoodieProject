namespace FoodieAPI.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";          // ví dụ FD482950
        public string CustomerName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = "cod";
        public string Status { get; set; } = "pending";
        public long Total { get; set; }                 // đã gồm phí giao hàng
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string SearchText { get; private set; } = "";
        public List<OrderItem> Items { get; set; } = [];

        public void UpdateSearchText() => SearchText = $"{Code} {CustomerName}".ToLowerInvariant();
    }
}