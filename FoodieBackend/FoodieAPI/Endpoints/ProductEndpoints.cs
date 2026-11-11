using Microsoft.EntityFrameworkCore;
using FoodieAPI.Data;
using FoodieAPI.DTOs;
using FoodieAPI.Common;
using FoodieAPI.Models;

namespace FoodieAPI.Endpoints
{
    public static class ProductEndpoints
    {
        private const int MaxUnpagedItems = 200;

        public static void MapProducts(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/products");
            group.MapGet("/", List);
            group.MapGet("/featured", Featured);
            group.MapGet("/{id:int}", GetById);
        }

        // Khách hàng chỉ thấy món thuộc danh mục đang hoạt động
        private static IQueryable<Product> Visible(AppDbContext db) =>
            db.Products.AsNoTracking().Include(p => p.Category).Where(p => p.Category.IsActive);

        private static async Task<IResult> List(
            AppDbContext db, int? page, int? pageSize, int? categoryId, string? category, string? keyword)
        {
            var query = ProductQueries.Filter(Visible(db), categoryId, category, keyword);

            if (page is not null)
                return Results.Ok(await ProductQueries.PagedAsync(query, page, pageSize));

            var all = await query
                .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
                .Take(MaxUnpagedItems)
                .ToListAsync();

            return Results.Ok(all.Select(p => p.ToDto()).ToList());
        }

        private static async Task<IResult> Featured(AppDbContext db)
        {
            var items = await Visible(db)
                .Where(p => p.IsFeatured)
                .OrderByDescending(p => p.Rating).ThenBy(p => p.Id)
                .Take(8)
                .ToListAsync();

            return Results.Ok(items.Select(p => p.ToDto()).ToList());
        }

        private static async Task<IResult> GetById(int id, AppDbContext db)
        {
            var product = await Visible(db).FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new ApiException(404, "Không tìm thấy món ăn. Món có thể đã bị xóa.");

            return Results.Ok(product.ToDto());
        }
    }

    public static class ProductQueries
    {
        /// <summary>Lọc theo danh mục (id hoặc tên) và từ khóa (không phân biệt dấu).</summary>
        public static IQueryable<Product> Filter(
            IQueryable<Product> query, int? categoryId, string? category, string? keyword)
        {
            if (categoryId is > 0)
                query = query.Where(p => p.CategoryId == categoryId);

            var cat = category?.Trim();
            if (!string.IsNullOrEmpty(cat))
                query = query.Where(p => p.Category.Name == cat);

            var kw = TextUtil.Keyword(keyword);
            if (kw is not null)
                query = query.Where(p => p.SearchText.Contains(kw));

            return query;
        }

        /// <summary>Phân trang; query phải đã Include(p => p.Category).</summary>
        public static async Task<Paged<ProductDto>> PagedAsync(IQueryable<Product> query, int? page, int? pageSize)
        {
            var (p, size, skip) = Paging.Normalize(page, pageSize);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
                .Skip(skip).Take(size)
                .ToListAsync();

            return new Paged<ProductDto>(items.Select(x => x.ToDto()).ToList(), total, p, size);
        }
    }
}
