using Microsoft.AspNetCore.Mvc;
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

        private static async Task<IResult> List(
            AppDbContext db, int? page, int? pageSize, string? category, string? keyword)
        {
            if (page is not null)
                return Results.Ok(await ProductQueries.PagedAsync(db, category, keyword, page, pageSize));

            var all = await ProductQueries.Filter(db.Products.AsNoTracking(), category, keyword)
                .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
                .Take(MaxUnpagedItems)
                .ToListAsync();

            return Results.Ok(all.Select(p => p.ToDto()).ToList());
        }

        private static async Task<IResult> Featured(AppDbContext db)
        {
            var items = await db.Products.AsNoTracking()
                .Where(p => p.IsFeatured)
                .OrderByDescending(p => p.Rating).ThenBy(p => p.Id)
                .Take(8)
                .ToListAsync();

            return Results.Ok(items.Select(p => p.ToDto()).ToList());
        }

        private static async Task<IResult> GetById(int id, AppDbContext db)
        {
            var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new ApiException(404, "Không tìm thấy món ăn. Món có thể đã bị xóa.");

            return Results.Ok(product.ToDto());
        }
    }

    public static class ProductQueries
    {
        public static IQueryable<Product> Filter(IQueryable<Product> query, string? category, string? keyword)
        {
            var cat = category?.Trim();
            if (!string.IsNullOrEmpty(cat))
                query = query.Where(p => p.Category == cat);

            var kw = TextUtil.Keyword(keyword);
            if (kw is not null)
                query = query.Where(p => p.SearchText.Contains(kw));

            return query;
        }

        public static async Task<Paged<ProductDto>> PagedAsync(
            AppDbContext db, string? category, string? keyword, int? page, int? pageSize)
        {
            var (p, size, skip) = Paging.Normalize(page, pageSize);
            var query = Filter(db.Products.AsNoTracking(), category, keyword);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
                .Skip(skip).Take(size)
                .ToListAsync();

            return new Paged<ProductDto>(items.Select(x => x.ToDto()).ToList(), total, p, size);
        }
    }
}