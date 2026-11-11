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
    }

    public sealed class StatsService(AppDbContext db) : IStatsService
    {
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
