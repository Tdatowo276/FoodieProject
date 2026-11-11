using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Endpoints
{
    /// <summary>Web API thực đơn công khai (khách hàng). Chỉ trả món / danh mục đang hoạt động.</summary>
    public static class ProductEndpoints
    {
        public static void MapProducts(this IEndpointRouteBuilder app)
        {
            var products = app.MapGroup("/api/products").WithTags("Thực đơn");

            // Có tham số page => trả về dạng phân trang { items, total, page, pageSize }; không có => mảng
            products.MapGet("/", async (IProductService service,
                    int? page, int? pageSize, int? categoryId, string? category, string? keyword) =>
                {
                    var query = new ProductQuery(page, pageSize, categoryId, category, keyword);
                    return page is not null
                        ? Results.Ok(await service.SearchAsync(query, includeHidden: false))
                        : Results.Ok(await service.ListAsync(query));
                })
                .WithSummary("Danh sách món (lọc, tìm kiếm, phân trang)")
                .WithDescription(
                    "keyword: tìm theo tên, không phân biệt dấu (\"pho\" khớp \"Phở\"). " +
                    "categoryId hoặc category (tên danh mục) để lọc. " +
                    "Có page => trả Paged<ProductDto> (pageSize 1-50, mặc định 10); không có page => mảng tối đa 200 món.")
                .Produces<Paged<ProductDto>>()
                .Produces<List<ProductDto>>();

            products.MapGet("/featured", (IProductService service) => service.GetFeaturedAsync())
                .WithSummary("Tối đa 8 món nổi bật, xếp theo đánh giá");

            products.MapGet("/{id:int}", (int id, IProductService service) => service.GetAsync(id, includeHidden: false))
                .WithSummary("Chi tiết một món")
                .ProducesProblem(StatusCodes.Status404NotFound);

            app.MapGet("/api/categories", (ICategoryService service) => service.GetAllAsync(includeInactive: false))
                .WithTags("Thực đơn")
                .WithSummary("Danh mục đang hiển thị (kèm số món)");
        }
    }
}
