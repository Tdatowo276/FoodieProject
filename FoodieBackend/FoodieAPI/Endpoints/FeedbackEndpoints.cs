using FoodieAPI.Common;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Endpoints
{
    /// <summary>Góp ý của khách hàng (gửi công khai) + quản lý góp ý (admin).</summary>
    public static class FeedbackEndpoints
    {
        public static void MapFeedback(this IEndpointRouteBuilder app)
        {
            // Không bắt buộc đăng nhập; đã đăng nhập thì gắn tài khoản. Giới hạn số lần gửi để chống spam.
            app.MapPost("/api/feedback", async (FeedbackRequest req, IFeedbackService s, ICurrentUser me) =>
                {
                    var created = await s.CreateAsync(req, me.Id);
                    return Results.Created($"/api/admin/feedback/{created.Id}", created);
                })
                .Validate<FeedbackRequest>()
                .RequireRateLimiting(RateLimitPolicies.Feedback)
                .WithTags("Góp ý")
                .WithSummary("Gửi góp ý cho chủ shop")
                .WithDescription("topic: food | delivery | service | website | other. rating 1-5 (không bắt buộc). Tối đa 5 góp ý / 10 phút / IP.")
                .Produces<FeedbackDto>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status429TooManyRequests);

            var admin = app.MapGroup("/api/admin/feedback")
                .WithTags("Quản trị - Góp ý")
                .RequireAuthorization(AuthenticationSetup.AdminPolicy)
                .RequiresAdmin();

            admin.MapGet("/", (IFeedbackService s, bool? unread, string? keyword, int? page, int? pageSize) =>
                    s.SearchAsync(new FeedbackQuery(unread, keyword, page, pageSize)))
                .WithSummary("Danh sách góp ý (chưa đọc trước, mới nhất trước)");

            admin.MapPatch("/{id:int}/read", (int id, bool? read, IFeedbackService s) => s.SetReadAsync(id, read ?? true))
                .WithSummary("Đánh dấu đã đọc (?read=false để đánh dấu chưa đọc)")
                .ProducesProblem(StatusCodes.Status404NotFound);

            admin.MapDelete("/{id:int}", async (int id, IFeedbackService s) =>
                {
                    await s.DeleteAsync(id);
                    return Results.NoContent();
                })
                .WithSummary("Xóa góp ý")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }
}
