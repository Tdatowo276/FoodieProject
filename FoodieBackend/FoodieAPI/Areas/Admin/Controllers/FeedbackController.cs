using Microsoft.AspNetCore.Mvc;
using FoodieAPI.Areas.Admin.Models;
using FoodieAPI.Common;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Areas.Admin.Controllers
{
    public class FeedbackController(IFeedbackService feedback) : AdminControllerBase
    {
        // GET /Admin/Feedback?unread=true&keyword=&page=   (AJAX => partial)
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] FeedbackQuery query)
        {
            query = RememberQuery("Feedback", query);
            var model = new FeedbackListViewModel { Query = query, Result = await feedback.SearchAsync(query) };
            return IsAjax ? PartialView("_FeedbackTable", model) : View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SetRead(int id, bool read = true)
        {
            try
            {
                await feedback.SetReadAsync(id, read);
            }
            catch (ApiException ex)
            {
                FlashError(ex.Message);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await feedback.DeleteAsync(id);
                FlashSuccess("Đã xóa góp ý.");
            }
            catch (ApiException ex)
            {
                FlashError(ex.Message);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
