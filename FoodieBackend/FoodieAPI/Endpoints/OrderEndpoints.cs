using FoodieAPI.Common;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Endpoints
{
    /// <summary>Web API đặt hàng (khách đã đăng nhập).</summary>
    public static class OrderEndpoints
    {
        public static void MapOrders(this IEndpointRouteBuilder app)
        {
            // Người đặt lấy từ token do server ký (ICurrentUser), không tin dữ liệu client gửi lên
            app.MapPost("/api/orders", async (OrderRequest req, IOrderService orders, ICurrentUser me) =>
                    Results.Json(await orders.CreateAsync(me.RequireId(), req), statusCode: StatusCodes.Status201Created))
                .RequireAuthorization()
                .Validate<OrderRequest>();
        }
    }
}
