using FoodieAPI.Common;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Endpoints
{
    /// <summary>
    /// Web API quản trị (JWT, vai trò Admin). Cùng tầng Service với khu Admin MVC.
    /// Quyền được kiểm tra phía server bằng policy "Admin", không dựa vào dữ liệu client gửi.
    /// </summary>
    public static class AdminEndpoints
    {
        public static void MapAdmin(this IEndpointRouteBuilder app)
        {
            var admin = app.MapGroup("/api/admin")
                .RequireAuthorization(AuthenticationSetup.AdminPolicy)
                .RequiresAdmin();

            admin.MapGet("/stats", (IStatsService stats) => stats.GetDashboardAsync())
                .WithTags("Quản trị - Tổng quan")
                .WithSummary("Doanh thu, số đơn, khách hàng tháng này so với tháng trước");

            MapProducts(admin.MapGroup("/products").WithTags("Quản trị - Món ăn"));
            MapCategories(admin.MapGroup("/categories").WithTags("Quản trị - Danh mục"));
            MapOrders(admin.MapGroup("/orders").WithTags("Quản trị - Đơn hàng"));
            MapUsers(admin.MapGroup("/users").WithTags("Quản trị - Người dùng"));

            // API dùng JWT trong header (không dùng cookie) nên không bị CSRF => tắt antiforgery cho form upload
            admin.MapPost("/uploads", async (IFormFile? file, IImageStorage images) =>
                    new UploadResult(await images.SaveAsync(file)))
                .DisableAntiforgery()
                .WithTags("Quản trị - Món ăn")
                .WithSummary("Upload ảnh món (multipart/form-data, field \"file\")")
                .WithDescription(
                    "Chỉ nhận .jpg/.jpeg/.png tối đa 2MB. Server kiểm tra nội dung thật của file (magic bytes) " +
                    "và tự đặt tên ngẫu nhiên. Trả về url dùng cho trường imageUrl của món.")
                .ProducesProblem(StatusCodes.Status400BadRequest);
        }

        private static void MapProducts(RouteGroupBuilder g)
        {
            g.MapGet("/", (IProductService s, int? page, int? pageSize, int? categoryId, string? category, string? keyword) =>
                    s.SearchAsync(new ProductQuery(page, pageSize, categoryId, category, keyword), includeHidden: true))
                .WithSummary("Danh sách món (gồm cả món thuộc danh mục đang ẩn)");

            g.MapGet("/{id:int}", (int id, IProductService s) => s.GetAsync(id, includeHidden: true))
                .WithSummary("Chi tiết món")
                .ProducesProblem(StatusCodes.Status404NotFound);

            g.MapPost("/", async (ProductRequest req, IProductService s) =>
                {
                    var created = await s.CreateAsync(req);
                    return Results.Created($"/api/products/{created.Id}", created);
                })
                .Validate<ProductRequest>()
                .WithSummary("Thêm món")
                .Produces<ProductDto>(StatusCodes.Status201Created)
                .ProducesValidationProblem();

            g.MapPut("/{id:int}", (int id, ProductRequest req, IProductService s) => s.UpdateAsync(id, req))
                .Validate<ProductRequest>()
                .WithSummary("Sửa món (ảnh cũ tự xóa nếu đổi ảnh)")
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status404NotFound);

            g.MapDelete("/{id:int}", async (int id, IProductService s) =>
                {
                    await s.DeleteAsync(id);
                    return Results.NoContent();
                })
                .WithSummary("Xóa món (đơn cũ vẫn giữ tên và giá món)")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status404NotFound);
        }

        private static void MapCategories(RouteGroupBuilder g)
        {
            g.MapGet("/", (ICategoryService s) => s.GetAllAsync(includeInactive: true))
                .WithSummary("Tất cả danh mục (gồm cả danh mục đang ẩn)");

            g.MapGet("/{id:int}", (int id, ICategoryService s) => s.GetAsync(id))
                .WithSummary("Chi tiết danh mục")
                .ProducesProblem(StatusCodes.Status404NotFound);

            g.MapPost("/", async (CategoryRequest req, ICategoryService s) =>
                {
                    var created = await s.CreateAsync(req);
                    return Results.Created($"/api/admin/categories/{created.Id}", created);
                })
                .Validate<CategoryRequest>()
                .WithSummary("Thêm danh mục (tên không được trùng)")
                .Produces<CategoryDto>(StatusCodes.Status201Created)
                .ProducesValidationProblem();

            g.MapPut("/{id:int}", (int id, CategoryRequest req, ICategoryService s) => s.UpdateAsync(id, req))
                .Validate<CategoryRequest>()
                .WithSummary("Sửa danh mục / ẩn hiện danh mục")
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status404NotFound);

            g.MapDelete("/{id:int}", async (int id, ICategoryService s) =>
                {
                    await s.DeleteAsync(id);
                    return Results.NoContent();
                })
                .WithSummary("Xóa danh mục (chỉ khi không còn món)")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }

        private static void MapOrders(RouteGroupBuilder g)
        {
            g.MapGet("/", (IOrderService s, int? page, int? pageSize, string? status, string? keyword) =>
                    s.SearchAsync(new OrderQuery(status, keyword, page, pageSize)))
                .WithSummary("Danh sách đơn (lọc trạng thái, tìm theo mã đơn / tên người nhận)")
                .WithDescription("status: pending | preparing | delivering | completed | cancelled.")
                .ProducesValidationProblem();

            g.MapGet("/{code}", (string code, IOrderService s) => s.GetDetailAsync(code, ownerUserId: null))
                .WithSummary("Chi tiết đơn (kèm các trạng thái được phép chuyển tới)")
                .ProducesProblem(StatusCodes.Status404NotFound);

            g.MapPatch("/{code}/status", (string code, OrderStatusRequest req, IOrderService s) =>
                    s.UpdateStatusAsync(code, req.Status))
                .Validate<OrderStatusRequest>()
                .WithSummary("Đổi trạng thái đơn")
                .WithDescription(
                    "Luồng hợp lệ: pending -> preparing -> delivering -> completed; " +
                    "pending/preparing -> cancelled. Sai luồng trả 400; admin khác vừa đổi trước trả 409.")
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict);
        }

        private static void MapUsers(RouteGroupBuilder g)
        {
            g.MapGet("/", (IUserService s, int? page, int? pageSize, string? keyword, string? role) =>
                    s.SearchAsync(new UserQuery(keyword, role, page, pageSize)))
                .WithSummary("Danh sách tài khoản (tìm theo email / họ tên, lọc vai trò)")
                .WithDescription("role: Admin | Customer.")
                .ProducesValidationProblem();

            g.MapPatch("/{id:int}/lock", (int id, SetLockedRequest req, IUserService s) => s.SetLockedAsync(id, req.Locked!.Value))
                .Validate<SetLockedRequest>()
                .WithSummary("Khóa / mở khóa tài khoản khách hàng")
                .WithDescription("Có hiệu lực ngay: token cũ của tài khoản bị khóa bị từ chối. Không khóa được tài khoản Admin.")
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }
}
