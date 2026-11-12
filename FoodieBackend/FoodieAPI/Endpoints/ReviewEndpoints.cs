using FoodieAPI.Common;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Endpoints
{
    /// <summary>Đánh giá món ăn + món bán chạy.</summary>
    public static class ReviewEndpoints
    {
        public static void MapReviews(this IEndpointRouteBuilder app)
        {
            var reviews = app.MapGroup("/api/products/{productId:int}/reviews").WithTags("Đánh giá món");

            reviews.MapGet("/", (int productId, IReviewService s, int? page, int? pageSize) =>
                    s.GetForProductAsync(productId, page, pageSize))
                .WithSummary("Đánh giá của một món + điểm trung bình, số lượt theo từng mức sao")
                .ProducesProblem(StatusCodes.Status404NotFound);

            // /me: đánh giá của chính người đang đăng nhập (người dùng lấy từ token, không có {userId} trên URL)
            var mine = reviews.MapGroup("/me").RequireAuthorization().RequiresLogin();

            mine.MapGet("/", (int productId, IReviewService s, ICurrentUser me) => s.GetMineAsync(productId, me.RequireId()))
                .WithSummary("Tôi đã được phép đánh giá chưa (đã mua và nhận hàng) + đánh giá hiện tại của tôi");

            mine.MapPut("/", (int productId, ReviewRequest req, IReviewService s, ICurrentUser me) =>
                    s.UpsertAsync(productId, me.RequireId(), req))
                .Validate<ReviewRequest>()
                .WithSummary("Thêm hoặc sửa đánh giá của tôi (1-5 sao, nhận xét tối đa 500 ký tự)")
                .WithDescription("Chỉ khách có đơn ĐÃ HOÀN THÀNH chứa món mới được đánh giá (403 nếu chưa). Mỗi khách 1 đánh giá / món.")
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound);

            mine.MapDelete("/", async (int productId, IReviewService s, ICurrentUser me) =>
                {
                    await s.DeleteMineAsync(productId, me.RequireId());
                    return Results.NoContent();
                })
                .WithSummary("Xóa đánh giá của tôi")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status404NotFound);

            // Món bán chạy cho trang chủ (chỉ món đang bán)
            app.MapGet("/api/products/best-sellers", (IStatsService stats, int? days, int? take) =>
                    stats.GetTopProductsAsync(days ?? 30, take ?? 8, onlyVisible: true))
                .WithTags("Thực đơn")
                .WithSummary("Món bán chạy (theo số phần đã bán trong các đơn hoàn thành, mặc định 30 ngày)");

            // ---------------- Quản trị ----------------
            var admin = app.MapGroup("/api/admin")
                .RequireAuthorization(AuthenticationSetup.AdminPolicy)
                .RequiresAdmin();

            admin.MapGet("/reviews", (IReviewService s, int? rating, string? keyword, int? page, int? pageSize) =>
                    s.SearchAsync(new ReviewQuery(rating, keyword, page, pageSize)))
                .WithTags("Quản trị - Đánh giá")
                .WithSummary("Danh sách đánh giá (lọc theo số sao, tìm theo món / người / nội dung)");

            admin.MapDelete("/reviews/{id:int}", async (int id, IReviewService s) =>
                {
                    await s.DeleteAsync(id);
                    return Results.NoContent();
                })
                .WithTags("Quản trị - Đánh giá")
                .WithSummary("Xóa đánh giá không phù hợp")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status404NotFound);

            admin.MapGet("/stats/top-products", (IStatsService stats, int? days, int? take) =>
                    stats.GetTopProductsAsync(days ?? 30, take ?? 5))
                .WithTags("Quản trị - Tổng quan")
                .WithSummary("Món bán chạy (số phần + doanh thu, chỉ đơn hoàn thành)");
        }
    }
}
