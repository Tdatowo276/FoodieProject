using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FoodieAPI.Controllers
{
    /// <summary>
    /// Trang lỗi chung cho phần MVC. UseExceptionHandler("/Error") và
    /// UseStatusCodePagesWithReExecute("/Error/{0}") chuyển request lỗi về đây.
    /// Không bao giờ hiển thị stack trace cho người dùng.
    /// </summary>
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]   // trang HTML, không phải REST API => không đưa vào Swagger
    [IgnoreAntiforgeryToken]   // request lỗi có thể là POST được chạy lại; không kiểm tra token lần nữa
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ErrorController(ILogger<ErrorController> logger) : Controller
    {
        [Route("Error")]
        public IActionResult Index()
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            // UseExceptionHandler đã log chi tiết lỗi; ở đây ghi thêm mã tra cứu để đối chiếu với người dùng
            logger.LogError("Lỗi máy chủ tại {Path}, mã tra cứu {TraceId}", feature?.Path, traceId);

            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return View("Error", new ErrorViewModel(500, "Đã xảy ra lỗi máy chủ",
                "Hệ thống gặp sự cố khi xử lý yêu cầu. Vui lòng thử lại sau.", traceId));
        }

        [Route("Error/{code:int}")]
        public IActionResult Status(int code)
        {
            var (title, message) = code switch
            {
                403 => ("Không có quyền truy cập", "Bạn không có quyền xem trang này."),
                404 => ("Không tìm thấy trang", "Trang bạn tìm không tồn tại hoặc đã bị xóa."),
                429 => ("Thao tác quá nhanh", "Bạn thao tác quá nhanh. Vui lòng thử lại sau ít phút."),
                _ => ("Yêu cầu không thành công", "Không thể xử lý yêu cầu của bạn.")
            };
            Response.StatusCode = code;
            return View("Error", new ErrorViewModel(code, title, message, null));
        }
    }

    public record ErrorViewModel(int StatusCode, string Title, string Message, string? TraceId);
}
