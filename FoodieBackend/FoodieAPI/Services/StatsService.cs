using Microsoft.EntityFrameworkCore;
using FoodieAPI.Common;
using FoodieAPI.Data;
using FoodieAPI.DTOs;

namespace FoodieAPI.Services
{
    /// <summary>Số liệu Dashboard: tháng này so với tháng trước.</summary>
    public interface IStatsService
    {
        Task<StatsDto> GetDashboardAsync();

        /// <summary>
        /// Món bán chạy trong <paramref name="days"/> ngày gần nhất, xếp theo số phần đã bán.
        /// Chỉ tính đơn ĐÃ HOÀN THÀNH (cùng quy tắc với doanh thu). onlyVisible = true: bỏ món đang ẩn (trang khách).
        /// </summary>
        Task<List<TopProductDto>> GetTopProductsAsync(int days = 30, int take = 5, bool onlyVisible = false);
    }

    public sealed class StatsService(AppDbContext db) : IStatsService
    {
        public async Task<List<TopProductDto>> GetTopProductsAsync(int days = 30, int take = 5, bool onlyVisible = false)
        {
            days = Math.Clamp(days, 1, 365);
            take = Math.Clamp(take, 1, 20);
            var from = DateTime.UtcNow.AddDays(-days);

            var items = db.OrderItems.AsNoTracking()
                .Where(i => i.ProductId != null                       // món đã bị xóa khỏi thực đơn thì bỏ qua
                         && i.Order.Status == OrderStatuses.Completed
                         && i.Order.CreatedAt >= from);
            if (onlyVisible) items = items.Where(i => i.Product!.Category.IsActive);

            // GROUP BY trên CSDL, chỉ lấy về vài dòng kết quả
            var top = await items
                .GroupBy(i => i.ProductId!.Value)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.UnitPrice * i.Quantity)
                })
                .OrderByDescending(x => x.Quantity).ThenByDescending(x => x.Revenue).ThenBy(x => x.ProductId)
                .Take(take)
                .ToListAsync();

            var ids = top.Select(x => x.ProductId).ToList();
            var products = await db.Products.AsNoTracking()
                .Where(p => ids.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => new { p.Name, p.ImageUrl });

            return top
                .Where(x => products.ContainsKey(x.ProductId))
                .Select(x => new TopProductDto(x.ProductId, products[x.ProductId].Name, products[x.ProductId].ImageUrl, x.Quantity, x.Revenue))
                .ToList();
        }

        public async Task<StatsDto> GetDashboardAsync()
        {
            var now = DateTime.UtcNow;
            var thisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var prevMonth = thisMonth.AddMonths(-1);
            var nextMonth = thisMonth.AddMonths(1);

            // Doanh thu chỉ tính đơn đã hoàn thành
            async Task<long> Revenue(DateTime from, DateTime to) =>
                await db.Orders
                    .Where(o => o.Status == OrderStatuses.Completed && o.CreatedAt >= from && o.CreatedAt < to)
                    .SumAsync(o => o.Total);

            async Task<int> ActiveOrders(DateTime from, DateTime to) =>
                await db.Orders.CountAsync(o =>
                    o.Status != OrderStatuses.Cancelled && o.CreatedAt >= from && o.CreatedAt < to);

            async Task<int> NewCustomers(DateTime from, DateTime to) =>
                await db.Users.CountAsync(u => u.Role == Roles.Customer && u.CreatedAt >= from && u.CreatedAt < to);

            var revenueNow = await Revenue(thisMonth, nextMonth);
            var revenuePrev = await Revenue(prevMonth, thisMonth);
            var ordersNow = await ActiveOrders(thisMonth, nextMonth);
            var ordersPrev = await ActiveOrders(prevMonth, thisMonth);
            var customersNow = await NewCustomers(thisMonth, nextMonth);
            var customersPrev = await NewCustomers(prevMonth, thisMonth);
            var totalCustomers = await db.Users.CountAsync(u => u.Role == Roles.Customer);

            return new StatsDto(
                new Metric(revenueNow, Growth(revenueNow, revenuePrev)),
                new Metric(ordersNow, Growth(ordersNow, ordersPrev)),
                new Metric(totalCustomers, Growth(customersNow, customersPrev)));
        }

        private static double Growth(long current, long previous) =>
            previous == 0
                ? (current > 0 ? 100 : 0)
                : Math.Round((double)(current - previous) / previous * 100, 1);
    }
}
