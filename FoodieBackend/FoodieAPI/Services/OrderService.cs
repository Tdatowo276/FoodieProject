using Microsoft.EntityFrameworkCore;
using FoodieAPI.Common;
using FoodieAPI.Data;
using FoodieAPI.DTOs;
using FoodieAPI.Models;

namespace FoodieAPI.Services
{
    public interface IOrderService
    {
        /// <summary>Tạo đơn cho user; giá lấy từ CSDL, không tin giá client gửi lên.</summary>
        Task<OrderCreated> CreateAsync(int userId, OrderRequest req);

        /// <summary>Danh sách đơn cho admin, lọc theo trạng thái / từ khóa (mã đơn, tên người nhận).</summary>
        Task<Paged<AdminOrderDto>> SearchAsync(OrderQuery query);

        /// <summary>Đơn của một khách hàng (trang "Đơn của tôi").</summary>
        Task<Paged<MyOrderDto>> GetForUserAsync(int userId, int? page, int? pageSize);

        /// <summary>
        /// Chi tiết đơn. ownerUserId = null: admin xem mọi đơn.
        /// ownerUserId có giá trị: chỉ xem được đơn của chính mình (đơn người khác trả 404, không lộ là có tồn tại).
        /// </summary>
        Task<OrderDetailDto> GetDetailAsync(string code, int? ownerUserId);

        /// <summary>Admin đổi trạng thái đơn theo luồng hợp lệ (xem OrderStatuses.CanChange).</summary>
        Task<OrderDetailDto> UpdateStatusAsync(string code, string? newStatus);
    }

    public sealed class OrderService(AppDbContext db, ILogger<OrderService> logger, ICurrentUser currentUser)
        : IOrderService
    {
        private const string NotFoundMessage = "Không tìm thấy đơn hàng.";

        public async Task<OrderCreated> CreateAsync(int userId, OrderRequest req)
        {
            // Định dạng đã được Data Annotations của OrderRequest kiểm tra (ValidationFilter)
            var receiver = req.ReceiverName!.Trim();
            var address = req.Address!.Trim();
            new RuleChecker()
                .Check(receiver.Length >= 2, "receiverName", "Tên người nhận cần từ 2 đến 100 ký tự.")
                .Check(address.Length >= 1, "address", "Vui lòng nhập địa chỉ giao hàng.")
                .ThrowIfInvalid();

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new ApiException(401, "Tài khoản không còn tồn tại. Vui lòng đăng nhập lại.");
            if (user.IsLocked)
                throw new ApiException(403, "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");

            // Gộp các dòng trùng món
            var lines = req.Items!
                .GroupBy(i => i.ProductId)
                .Select(g => (ProductId: g.Key, Quantity: g.Sum(x => x.Quantity)))
                .ToList();

            if (lines.Any(l => l.Quantity > 99))
                throw ApiException.Validation("items", "Mỗi món tối đa 99 phần.");

            // Chỉ cho đặt món thuộc danh mục đang hoạt động
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
                Phone = req.Phone!.Trim(),
                Address = address,
                Note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim(),
                PaymentMethod = req.PaymentMethod!,
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
            order.Total = subtotal + ShippingFeeFor(subtotal);
            order.Code = await GenerateCodeAsync();
            order.UpdateSearchText();

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            logger.LogInformation("Người dùng {UserId} tạo đơn {OrderCode}, tổng {Total}đ, {ItemCount} món",
                userId, order.Code, order.Total, order.Items.Count);

            return new OrderCreated(order.Code, order.Total, order.Status, order.CreatedAt);
        }

        public async Task<Paged<AdminOrderDto>> SearchAsync(OrderQuery query)
        {
            if (!string.IsNullOrWhiteSpace(query.Status) && !OrderStatuses.IsValid(query.Status))
                throw ApiException.Validation("status", "Trạng thái đơn hàng không hợp lệ.");

            var (page, size, skip) = Paging.Normalize(query.Page, query.PageSize);
            var q = db.Orders.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Status))
                q = q.Where(o => o.Status == query.Status);

            var kw = TextUtil.Keyword(query.Keyword);
            if (kw is not null)
                q = q.Where(o => o.SearchText.Contains(kw));

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
                .Skip(skip).Take(size)
                .ToListAsync();

            return new Paged<AdminOrderDto>(items.Select(o => o.ToAdminDto()).ToList(), total, page, size);
        }

        public async Task<Paged<MyOrderDto>> GetForUserAsync(int userId, int? page, int? pageSize)
        {
            var (p, size, skip) = Paging.Normalize(page, pageSize);
            var q = db.Orders.AsNoTracking().Where(o => o.UserId == userId);

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
                .Skip(skip).Take(size)
                .Select(o => new MyOrderDto(o.Code, o.CreatedAt, o.Total, o.Status, o.Items.Sum(i => i.Quantity)))
                .ToListAsync();

            return new Paged<MyOrderDto>(items, total, p, size);
        }

        public async Task<OrderDetailDto> GetDetailAsync(string code, int? ownerUserId)
        {
            var order = await db.Orders.AsNoTracking()
                .Include(o => o.Items)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Code == code && (ownerUserId == null || o.UserId == ownerUserId))
                ?? throw ApiException.NotFound(NotFoundMessage);

            return ToDetail(order);
        }

        public async Task<OrderDetailDto> UpdateStatusAsync(string code, string? newStatus)
        {
            if (!OrderStatuses.IsValid(newStatus))
                throw ApiException.Validation("status", "Trạng thái đơn hàng không hợp lệ.");

            var current = await db.Orders.AsNoTracking()
                .Where(o => o.Code == code)
                .Select(o => o.Status)
                .FirstOrDefaultAsync()
                ?? throw ApiException.NotFound(NotFoundMessage);

            if (current == newStatus) return await GetDetailAsync(code, ownerUserId: null);

            if (!OrderStatuses.CanChange(current, newStatus!))
                throw ApiException.Validation("status",
                    $"Không thể chuyển đơn từ \"{OrderStatuses.Label(current)}\" sang \"{OrderStatuses.Label(newStatus!)}\".");

            // UPDATE có điều kiện "trạng thái vẫn như lúc vừa đọc": nếu admin khác đã đổi trước
            // thì không ghi đè mà báo xung đột
            var updated = await db.Orders
                .Where(o => o.Code == code && o.Status == current)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, newStatus));
            if (updated == 0)
                throw ApiException.Conflict("Đơn hàng vừa được người khác cập nhật. Vui lòng tải lại.");

            logger.LogInformation("{Actor} chuyển đơn {OrderCode}: {Old} -> {New}",
                currentUser.Describe(), code, current, newStatus);
            return await GetDetailAsync(code, ownerUserId: null);
        }

        // ---------------------------------------------------------------------

        public static long ShippingFeeFor(long subtotal) =>
            subtotal >= Limits.FreeShippingFrom ? 0 : Limits.ShippingFee;

        private static OrderDetailDto ToDetail(Order o)
        {
            var items = o.Items
                .OrderBy(i => i.Id)
                .Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.UnitPrice * i.Quantity))
                .ToList();
            var subtotal = items.Sum(i => i.LineTotal);

            return new OrderDetailDto(
                o.Code, o.CustomerName, o.Phone, o.Address, o.Note, o.PaymentMethod, o.Status,
                subtotal, o.Total - subtotal, o.Total, o.CreatedAt, o.User.Email,
                OrderStatuses.NextOf(o.Status), items);
        }

        private async Task<string> GenerateCodeAsync()
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
