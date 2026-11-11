using Microsoft.AspNetCore.Mvc;
using FoodieAPI.Services;

namespace FoodieAPI.Areas.Admin.Controllers
{
    public class DashboardController(IStatsService stats) : AdminControllerBase
    {
        public async Task<IActionResult> Index() => View(await stats.GetDashboardAsync());
    }
}
