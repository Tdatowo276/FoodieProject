using System.Text;
using Serilog;
using Serilog.Events;

namespace FoodieAPI.Common
{
    /// <summary>
    /// Ghi log (Serilog) ra console và ra file trong thư mục logs/ của project:
    ///  - logs/foodie-YYYYMMDD.txt : mọi sự kiện từ mức Information (đăng nhập, đặt hàng, thao tác admin,
    ///                               mỗi request HTTP kèm mã kết quả và thời gian xử lý)
    ///  - logs/errors-YYYYMMDD.txt : chỉ cảnh báo + lỗi (đăng nhập sai, tài khoản bị khóa, lỗi máy chủ...)
    /// Mỗi ngày một file, giữ 14 ngày gần nhất. Thư mục logs/ không đưa lên Git.
    /// Tắt ghi file bằng cấu hình "FileLogging:Enabled" = false (bộ kiểm thử tự động dùng cách này).
    /// </summary>
    public static class LoggingSetup
    {
        private const string Template =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

        public static WebApplicationBuilder AddFoodieLogging(this WebApplicationBuilder builder)
        {
            var fileEnabled = builder.Configuration.GetValue("FileLogging:Enabled", true);
            var logFolder = Path.Combine(builder.Environment.ContentRootPath, "logs");

            builder.Host.UseSerilog((context, logger) =>
            {
                logger
                    .MinimumLevel.Information()
                    // Bớt log chi tiết của framework, giữ lại thông báo khởi động và cảnh báo
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: Template);

                if (!fileEnabled) return;

                logger
                    .WriteTo.File(
                        Path.Combine(logFolder, "foodie-.txt"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 14,
                        outputTemplate: Template,
                        encoding: Encoding.UTF8,
                        shared: true)
                    .WriteTo.File(
                        Path.Combine(logFolder, "errors-.txt"),
                        restrictedToMinimumLevel: LogEventLevel.Warning,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 14,
                        outputTemplate: Template,
                        encoding: Encoding.UTF8,
                        shared: true);
            });

            return builder;
        }

        /// <summary>
        /// Mỗi request HTTP ghi 1 dòng: phương thức, đường dẫn, mã kết quả, thời gian xử lý, người gọi.
        /// Đặt ĐẦU pipeline (ngoài middleware xử lý lỗi) để ghi đúng mã kết quả cuối cùng
        /// (ví dụ lỗi dữ liệu là 400, không bị ghi nhầm thành 500). File tĩnh thành công (ảnh, css) không ghi.
        /// </summary>
        public static IApplicationBuilder UseFoodieRequestLogging(this IApplicationBuilder app) =>
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate =
                    "HTTP {RequestMethod} {RequestPath} => {StatusCode} ({Elapsed:0} ms) bởi {User}";
                options.EnrichDiagnosticContext = (diagnostics, http) =>
                    diagnostics.Set("User", http.User.Identity?.IsAuthenticated == true
                        ? $"user#{http.User.FindFirst("sub")?.Value} ({http.User.FindFirst("role")?.Value})"
                        : "khách");
                // 5xx là lỗi, 4xx là cảnh báo (vào file errors), còn lại là thông tin
                options.GetLevel = (http, _, ex) =>
                    ex is not null || http.Response.StatusCode >= 500 ? LogEventLevel.Error
                    : http.Response.StatusCode >= 400 ? LogEventLevel.Warning
                    // File tĩnh (không có endpoint) tải thành công: mức Debug => không ghi
                    : http.GetEndpoint() is null ? LogEventLevel.Debug
                    : LogEventLevel.Information;
            });
    }
}
