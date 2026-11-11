using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using FoodieAPI.DTOs;

namespace FoodieAPI.Areas.Admin.Models
{
    /// <summary>
    /// Thông tin phân trang. RouteValues giữ nguyên bộ lọc hiện tại khi chuyển trang
    /// (kể cả bộ lọc được khôi phục từ Session).
    /// </summary>
    public record PagerModel(int Page, int PageSize, int Total, IDictionary<string, string?> RouteValues)
    {
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
    }

    // ===================== Món ăn =====================

    public class ProductListViewModel
    {
        public ProductQuery Query { get; init; } = new();
        public Paged<ProductDto> Result { get; init; } = new([], 0, 1, 10);
        public List<CategoryDto> Categories { get; init; } = [];

        public PagerModel Pager => new(Result.Page, Result.PageSize, Result.Total, new Dictionary<string, string?>
        {
            ["keyword"] = Query.Keyword,
            ["categoryId"] = Query.CategoryId?.ToString()
        });
    }

    public class ProductFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên món.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Tên món cần từ 2 đến 80 ký tự.")]
        [Display(Name = "Tên món")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        [Display(Name = "Danh mục")]
        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá.")]
        [Range(1_000, 10_000_000, ErrorMessage = "Giá phải từ 1.000đ đến 10.000.000đ.")]
        [Display(Name = "Giá (đ)")]
        public long? Price { get; set; }

        [StringLength(300, ErrorMessage = "Mô tả tối đa 300 ký tự.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Món nổi bật (hiện ở trang chủ)")]
        public bool IsFeatured { get; set; }

        // Ảnh được kiểm tra đuôi, kích thước và nội dung thật ở ImageStorage (phía server)
        [Display(Name = "Ảnh món (JPG/PNG, tối đa 2MB)")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Xóa ảnh hiện tại")]
        public bool RemoveImage { get; set; }

        // Chỉ để hiển thị; khi lưu, ảnh hiện tại luôn được đọc lại từ CSDL (không tin giá trị client gửi)
        public string? CurrentImageUrl { get; set; }

        public List<SelectListItem> Categories { get; set; } = [];

        public ProductRequest ToRequest(string? imageUrl) =>
            new(Name, CategoryId, Price, Description, imageUrl, IsFeatured);

        public static ProductFormViewModel From(ProductDto p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            CategoryId = p.CategoryId,
            Price = p.Price,
            Description = p.Description,
            IsFeatured = p.IsFeatured,
            CurrentImageUrl = p.ImageUrl
        };
    }

    // ===================== Danh mục =====================

    public class CategoryFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Tên danh mục cần từ 2 đến 50 ký tự.")]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = "";

        [StringLength(200, ErrorMessage = "Mô tả tối đa 200 ký tự.")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Hiển thị cho khách hàng")]
        public bool IsActive { get; set; } = true;

        public CategoryRequest ToRequest() => new(Name, Description, IsActive);

        public static CategoryFormViewModel From(CategoryDto c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            IsActive = c.IsActive
        };
    }

    // ===================== Đơn hàng =====================

    public class OrderListViewModel
    {
        public OrderQuery Query { get; init; } = new();
        public Paged<AdminOrderDto> Result { get; init; } = new([], 0, 1, 10);

        public PagerModel Pager => new(Result.Page, Result.PageSize, Result.Total, new Dictionary<string, string?>
        {
            ["keyword"] = Query.Keyword,
            ["status"] = Query.Status
        });
    }

    // ===================== Người dùng =====================

    public class UserListViewModel
    {
        public UserQuery Query { get; init; } = new();
        public Paged<UserDto> Result { get; init; } = new([], 0, 1, 10);

        public PagerModel Pager => new(Result.Page, Result.PageSize, Result.Total, new Dictionary<string, string?>
        {
            ["keyword"] = Query.Keyword,
            ["role"] = Query.Role
        });
    }

    // ===================== Dashboard =====================

    public class DashboardViewModel
    {
        public required StatsDto Stats { get; init; }
        public required IReadOnlyList<AdminOrderDto> PendingOrders { get; init; }
        public int PendingTotal { get; init; }
    }
}
