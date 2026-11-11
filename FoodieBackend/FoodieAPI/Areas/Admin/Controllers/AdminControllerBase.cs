using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodieAPI.Common;

namespace FoodieAPI.Areas.Admin.Controllers
{
    /// <summary>
    /// Lớp cơ sở cho mọi controller trong khu Admin:
    ///  - Bắt buộc đăng nhập bằng Cookie với vai trò Admin (kiểm tra phía server).
    ///  - Anti-forgery được kiểm tra tự động cho mọi POST (AutoValidateAntiforgeryToken toàn cục).
    ///  - Tiện ích thông báo qua TempData (mẫu Post-Redirect-Get).
    /// </summary>
    [Area("Admin")]
    [Authorize(Policy = AuthenticationSetup.AdminPolicy)]
    [ApiExplorerSettings(IgnoreApi = true)]   // trang HTML, không phải REST API => không đưa vào Swagger
    public abstract class AdminControllerBase : Controller
    {
        public const string SuccessKey = "Flash.Success";
        public const string ErrorKey = "Flash.Error";

        /// <summary>Thông báo hiển thị một lần ở trang kế tiếp (TempData sống qua 1 lần redirect).</summary>
        protected void FlashSuccess(string message) => TempData[SuccessKey] = message;
        protected void FlashError(string message) => TempData[ErrorKey] = message;

        /// <summary>Chuyển lỗi nghiệp vụ từ Service thành lỗi ModelState để hiển thị ngay trên form.</summary>
        protected void AddErrors(ApiException ex)
        {
            if (ex.Errors is null || ex.Errors.Count == 0)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return;
            }

            foreach (var (field, messages) in ex.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(ToPropertyName(field), message);
        }

        /// <summary>Request AJAX từ admin.js (trả về partial view thay vì cả trang).</summary>
        protected bool IsAjax => Request.Headers.XRequestedWith == "XMLHttpRequest";

        /// <summary>
        /// Ghi nhớ bộ lọc danh sách trong Session. Có tham số trên URL thì lưu lại;
        /// quay lại trang không có tham số (ví dụ sau khi sửa xong một món) thì khôi phục bộ lọc cũ.
        /// </summary>
        protected T RememberQuery<T>(string key, T query) where T : class
        {
            var sessionKey = "Filter." + key;
            if (Request.Query.Count > 0)
            {
                HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(query));
                return query;
            }

            var saved = HttpContext.Session.GetString(sessionKey);
            return saved is null ? query : JsonSerializer.Deserialize<T>(saved) ?? query;
        }

        // "categoryId" (key camelCase của API) -> "CategoryId" (tên thuộc tính ViewModel)
        private static string ToPropertyName(string field) =>
            string.IsNullOrEmpty(field) ? field : char.ToUpperInvariant(field[0]) + field[1..];
    }
}
