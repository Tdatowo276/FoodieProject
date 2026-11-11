using Microsoft.AspNetCore.Mvc;
using FoodieAPI.Areas.Admin.Models;
using FoodieAPI.Common;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Areas.Admin.Controllers
{
    public class OrdersController(IOrderService orders) : AdminControllerBase
    {
        // GET /Admin/Orders?status=&keyword=&page=   (AJAX => partial)
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] OrderQuery query)
        {
            query = RememberQuery("Orders", query);

            Paged<AdminOrderDto> result;
            try
            {
                result = await orders.SearchAsync(query);
            }
            catch (ApiException ex)
            {
                // Trạng thái lọc không hợp lệ (sửa tay trên URL) => bỏ lọc trạng thái
                FlashError(ex.Errors?.Values.FirstOrDefault()?.FirstOrDefault() ?? ex.Message);
                query = query with { Status = null };
                result = await orders.SearchAsync(query);
            }

            var model = new OrderListViewModel { Query = query, Result = result };
            return IsAjax ? PartialView("_OrderTable", model) : View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                return View(await orders.GetDetailAsync(id, ownerUserId: null));
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string id, string? status)
        {
            try
            {
                var order = await orders.UpdateStatusAsync(id, status);
                FlashSuccess($"Đơn {order.Code} chuyển sang \"{OrderStatuses.Label(order.Status)}\".");
            }
            catch (ApiException ex) when (ex.StatusCode == 404)
            {
                return NotFound();
            }
            catch (ApiException ex)
            {
                FlashError(ex.Errors?.Values.FirstOrDefault()?.FirstOrDefault() ?? ex.Message);
            }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
