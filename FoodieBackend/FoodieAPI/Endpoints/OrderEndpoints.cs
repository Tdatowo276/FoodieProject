using FoodieAPI.Common;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Endpoints
{
    /// <summary>Web API đơn hàng của khách đã đăng nhập.</summary>
    public static class OrderEndpoints
    {
        public static void MapOrders(this IEndpointRouteBuilder app)
        {
            // Người dùng luôn lấy từ token do server ký (ICurrentUser), không tin dữ liệu client gửi lên
            var orders = app.MapGroup("/api/orders").WithTags("Đơn hàng").RequireAuthorization().RequiresLogin();

            orders.MapPost("/", async (OrderRequest req, IOrderService service, ICurrentUser me) =>
                {
                    var created = await service.CreateAsync(me.RequireId(), req);
                    return Results.Created($"/api/orders/{created.OrderCode}", created);
                })
                .Validate<OrderRequest>()
                .WithSummary("Đặt hàng")
                .WithDescription(
                    "Giá món lấy từ CSDL, không nhận giá từ client. Phí giao 15.000đ, miễn phí từ 200.000đ. " +
                    "paymentMethod: cod | bank. Tài khoản bị khóa trả 403.")
                .Produces<OrderCreated>(StatusCodes.Status201Created)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status403Forbidden);

            orders.MapGet("/me", (IOrderService service, ICurrentUser me, int? page, int? pageSize) =>
                    service.GetForUserAsync(me.RequireId(), page, pageSize))
                .WithSummary("Đơn hàng của tôi (mới nhất trước, phân trang)");

            orders.MapGet("/{code}", (string code, IOrderService service, ICurrentUser me) =>
                    service.GetDetailAsync(code, ownerUserId: me.RequireId()))
                .WithSummary("Chi tiết một đơn của tôi")
                .WithDescription("Đơn của người khác trả 404 (không tiết lộ đơn đó có tồn tại hay không).")
                .ProducesProblem(StatusCodes.Status404NotFound);
        }
    }
}
