using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Endpoints
{
    /// <summary>Web API thực đơn công khai (khách hàng). Chỉ trả món thuộc danh mục đang hoạt động.</summary>
    public static class ProductEndpoints
    {
        public static void MapProducts(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/products");

            // Có tham số page => trả về dạng phân trang { items, total, page, pageSize }; không có => mảng
            group.MapGet("/", async (IProductService products,
                int? page, int? pageSize, int? categoryId, string? category, string? keyword) =>
            {
                var query = new ProductQuery(page, pageSize, categoryId, category, keyword);
                return page is not null
                    ? Results.Ok(await products.SearchAsync(query, includeHidden: false))
                    : Results.Ok(await products.ListAsync(query));
            });

            group.MapGet("/featured", (IProductService products) => products.GetFeaturedAsync());

            group.MapGet("/{id:int}", (int id, IProductService products) => products.GetAsync(id, includeHidden: false));
        }
    }
}
