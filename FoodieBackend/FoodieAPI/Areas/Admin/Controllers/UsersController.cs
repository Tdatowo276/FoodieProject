using Microsoft.AspNetCore.Mvc;
using FoodieAPI.Areas.Admin.Models;
using FoodieAPI.Common;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Areas.Admin.Controllers
{
    public class UsersController(IUserService users) : AdminControllerBase
    {
        // GET /Admin/Users?keyword=&role=&page=   (AJAX => partial)
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] UserQuery query)
        {
            query = RememberQuery("Users", query);

            Paged<UserDto> result;
            try
            {
                result = await users.SearchAsync(query);
            }
            catch (ApiException ex)
            {
                FlashError(ex.Errors?.Values.FirstOrDefault()?.FirstOrDefault() ?? ex.Message);
                query = query with { Role = null };
                result = await users.SearchAsync(query);
            }

            var model = new UserListViewModel { Query = query, Result = result };
            return IsAjax ? PartialView("_UserTable", model) : View(model);
        }

        /// <summary>
        /// Khóa / mở khóa tài khoản qua AJAX (admin.js). Anti-forgery token gửi trong header X-CSRF-TOKEN.
        /// Trả JSON để cập nhật dòng trong bảng mà không tải lại trang.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SetLocked(int id, bool locked)
        {
            try
            {
                var user = await users.SetLockedAsync(id, locked);
                return Json(new
                {
                    user.Id,
                    user.IsLocked,
                    message = user.IsLocked ? $"Đã khóa tài khoản {user.Email}." : $"Đã mở khóa tài khoản {user.Email}."
                });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
