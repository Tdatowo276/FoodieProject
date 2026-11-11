using FoodieAPI.Common;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Endpoints
{
    /// <summary>Web API cho quản trị viên. Toàn bộ logic nằm trong tầng Service.</summary>
    public static class AdminEndpoints
    {
        public static void MapAdmin(this IEndpointRouteBuilder app)
        {
            var admin = app.MapGroup("/api/admin").RequireAuthorization("Admin");

            admin.MapGet("/stats", (IStatsService stats) => stats.GetDashboardAsync());

            admin.MapGet("/orders", (IOrderService orders, int? page, int? pageSize, string? status, string? keyword) =>
                orders.SearchAsync(new OrderQuery(status, keyword, page, pageSize)));

            admin.MapGet("/products",
                (IProductService products, int? page, int? pageSize, int? categoryId, string? category, string? keyword) =>
                    products.SearchAsync(new ProductQuery(page, pageSize, categoryId, category, keyword), includeHidden: true));

            admin.MapPost("/products", async (ProductRequest req, IProductService products) =>
            {
                var created = await products.CreateAsync(req);
                return Results.Created($"/api/products/{created.Id}", created);
            }).Validate<ProductRequest>();

            admin.MapPut("/products/{id:int}", (int id, ProductRequest req, IProductService products) =>
                products.UpdateAsync(id, req)).Validate<ProductRequest>();

            admin.MapDelete("/products/{id:int}", async (int id, IProductService products) =>
            {
                await products.DeleteAsync(id);
                return Results.NoContent();
            });

            // API dùng JWT trong header (không dùng cookie) nên không bị CSRF; tắt antiforgery cho form upload
            admin.MapPost("/uploads", async (IFormFile? file, IImageStorage images) =>
                new UploadResult(await images.SaveAsync(file))).DisableAntiforgery();
        }
    }
}
