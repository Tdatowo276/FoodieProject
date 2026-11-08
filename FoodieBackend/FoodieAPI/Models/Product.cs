namespace FoodieAPI.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; private set; } = "";
        public string Category { get; set; } = "";
        public long Price { get; set; }                 // VND, số nguyên
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public double Rating { get; set; }              
        public bool IsFeatured { get; set; }            
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Tên không dấu, viết thường phục vụ tìm kiếm
        public string SearchText { get; private set; } = "";

        public void SetName(string name)
        {
            Name = name.Trim();
            // Nếu bạn tách TextUtil ra class chung, gọi TextUtil.Normalize ở đây
            SearchText = name.Trim().ToLowerInvariant(); 
        }
    }
}