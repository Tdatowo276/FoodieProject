using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using FoodieAPI.Data;
using FoodieAPI.Models;
using FoodieAPI.DTOs;
using FoodieAPI.Common;

namespace FoodieAPI.Endpoints
{
    public static class OrderEndpoints
    {
        public static void MapOrders(this IEndpointRouteBuilder app) =>
            app.MapPost("/api/orders", Create).RequireAuthorization().Validate<OrderRequest>();

        private static async Task<IResult> Create(
            OrderRequest req, ClaimsPrincipal principal, AppDbContext db, ILoggerFactory loggerFactory)
        {
            // Định dạng đã được Data Annotations của OrderRequest kiểm tra (ValidationFilter)
            var items = req.Items!;
            var receiver = req.ReceiverName!.Trim();
            var phone = req.Phone!.Trim();
            var address = req.Address!.Trim();
            var note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim();
            var payment = req.PaymentMethod!;

            new Validator()
                .Check(receiver.Length >= 2, "receiverName", "Tên người nhận cần từ 2 đến 100 ký tự.")
                .Check(address.Length >= 1, "address", "Vui lòng nhập địa chỉ giao hàng.")
                .ThrowIfInvalid();

            // Người đặt lấy từ token do server ký, không tin dữ liệu client gửi lên
            if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
                throw new ApiException(401, "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new ApiException(401, "Tài khoản không còn tồn tại. Vui lòng đăng nhập lại.");
            if (user.IsLocked)
                throw new ApiException(403, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");

            var lines = items
                .GroupBy(i => i.ProductId)
                .Select(g => (ProductId: g.Key, Quantity: g.Sum(x => x.Quantity)))
                .ToList();

            if (lines.Any(l => l.Quantity > 99))
                throw ApiException.Validation("items", "Mỗi món tối đa 99 phần.");

            // Chỉ cho đặt món thuộc danh mục đang hoạt động; giá lấy từ CSDL, không tin giá client gửi
            var ids = lines.Select(l => l.ProductId).ToList();
            var products = await db.Products.AsNoTracking()
                .Where(p => ids.Contains(p.Id) && p.Category.IsActive)
                .ToDictionaryAsync(p => p.Id);

            if (lines.Any(l => !products.ContainsKey(l.ProductId)))
                throw new ApiException(400, "Có món trong giỏ không còn bán. Vui lòng tải lại thực đơn.");

            var order = new Order
            {
                UserId = userId,
                CustomerName = receiver,
                Phone = phone,
                Address = address,
                Note = note,
                PaymentMethod = payment,
                Status = OrderStatuses.Pending,
                CreatedAt = DateTime.UtcNow,
                Items = lines.Select(l => new OrderItem
                {
                    ProductId = l.ProductId,
                    ProductName = products[l.ProductId].Name,
                    UnitPrice = products[l.ProductId].Price,
                    Quantity = l.Quantity
                }).ToList()
            };

            var subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
            var shipping = subtotal >= Limits.FreeShippingFrom ? 0 : Limits.ShippingFee;
            order.Total = subtotal + shipping;

            order.Code = await GenerateCodeAsync(db);
            order.UpdateSearchText();

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            loggerFactory.CreateLogger("FoodieAPI.Orders").LogInformation(
                "Người dùng {UserId} tạo đơn {OrderCode}, tổng {Total}đ, {ItemCount} món",
                userId, order.Code, order.Total, order.Items.Count);

            return Results.Json(
                new OrderCreated(order.Code, order.Total, order.Status, order.CreatedAt),
                statusCode: StatusCodes.Status201Created);
        }

        private static async Task<string> GenerateCodeAsync(AppDbContext db)
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var code = $"FD{Random.Shared.Next(100_000, 1_000_000)}";
                if (!await db.Orders.AnyAsync(o => o.Code == code)) return code;
            }

            throw new InvalidOperationException("Không tạo được mã đơn hàng duy nhất.");
        }
    }
}