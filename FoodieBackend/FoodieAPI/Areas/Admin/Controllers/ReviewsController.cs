using Microsoft.AspNetCore.Mvc;
using FoodieAPI.Areas.Admin.Models;
using FoodieAPI.Common;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Areas.Admin.Controllers
{
    /// <summary>Kiểm duyệt đánh giá món ăn.</summary>
    public class ReviewsController(IReviewService reviews) : AdminControllerBase
    {
        // GET /Admin/Reviews?rating=&keyword=&page=   (AJAX => partial)
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] ReviewQuery query)
        {
            query = RememberQuery("Reviews", query);
            var model = new ReviewListViewModel { Query = query, Result = await reviews.SearchAsync(query) };
            return IsAjax ? PartialView("_ReviewTable", model) : View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await reviews.DeleteAsync(id);
                FlashSuccess("Đã xóa đánh giá. Điểm trung bình của món đã được tính lại.");
            }
            catch (ApiException ex)
            {
                FlashError(ex.Message);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
