using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodieAPI.Data;
using FoodieAPI.Models;
using FoodieAPI.DTOs;
using FoodieAPI.Common;

namespace FoodieAPI.Endpoints
{
    public static class OrderEndpoints
    {
        public static void MapOrders(this IEndpointRouteBuilder app) =>
            app.MapPost("/api/orders", Create).RequireAuthorization();

        private static async Task<IResult> Create(OrderRequest req, AppDbContext db)
        {
            var items = req.Items ?? new List<OrderItemRequest>();
            var receiver = (req.ReceiverName ?? "").Trim();
            var phone = (req.Phone ?? "").Trim();
            var address = (req.Address ?? "").Trim();
            var note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim();
            var payment = req.PaymentMethod ?? "";

            new Validator()
                .Check(items.Count is >= 1 and <= 50, "items", "Giỏ hàng trống hoặc vượt quá 50 món.")
                .Check(items.All(i => i.ProductId > 0 && i.Quantity is >= 1 and <= 99),
                    "items", "Mã món hoặc số lượng không hợp lệ (mỗi món từ 1 đến 99 phần).")
                .Check(receiver.Length is >= 2 and <= 100, "receiverName", "Tên người nhận cần từ 2 đến 100 ký tự.")
                .Check(Patterns.Phone.IsMatch(phone), "phone", "Số điện thoại chưa đúng (ví dụ 0901234567).")
                .Check(address.Length is >= 1 and <= 250, "address", "Vui lòng nhập địa chỉ giao hàng (tối đa 250 ký tự).")
                .Check((note?.Length ?? 0) <= 500, "note", "Ghi chú tối đa 500 ký tự.")
                .Check(PaymentMethods.IsValid(payment), "paymentMethod", "Phương thức thanh toán không hợp lệ.")
                .ThrowIfInvalid();

            var lines = items
                .GroupBy(i => i.ProductId)
                .Select(g => (ProductId: g.Key, Quantity: g.Sum(x => x.Quantity)))
                .ToList();

            if (lines.Any(l => l.Quantity > 99))
                throw ApiException.Validation("items", "Mỗi món tối đa 99 phần.");

            var ids = lines.Select(l => l.ProductId).ToList();
            var products = await db.Products.AsNoTracking()
                .Where(p => ids.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            if (lines.Any(l => !products.ContainsKey(l.ProductId)))
                throw new ApiException(400, "Có món trong giỏ không còn bán. Vui lòng tải lại thực đơn.");

            var order = new Order
            {
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