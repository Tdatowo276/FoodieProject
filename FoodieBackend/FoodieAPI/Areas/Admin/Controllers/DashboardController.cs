using Microsoft.AspNetCore.Mvc;
using FoodieAPI.Areas.Admin.Models;
using FoodieAPI.Common;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Areas.Admin.Controllers
{
    public class DashboardController(IStatsService stats, IOrderService orders) : AdminControllerBase
    {
        public async Task<IActionResult> Index()
        {
            var pending = await orders.SearchAsync(new OrderQuery(Status: OrderStatuses.Pending, PageSize: 5));
            return View(new DashboardViewModel
            {
                Stats = await stats.GetDashboardAsync(),
                TopProducts = await stats.GetTopProductsAsync(days: 30, take: 5),
                PendingOrders = pending.Items,
                PendingTotal = pending.Total
            });
        }
    }
}
