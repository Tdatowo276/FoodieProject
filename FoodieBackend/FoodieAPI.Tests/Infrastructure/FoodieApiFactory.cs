using FoodieAPI.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodieAPI.Tests.Infrastructure
{
    /// <summary>
    /// Khởi động toàn bộ ứng dụng Foodie trong bộ nhớ (TestServer) để kiểm thử tích hợp qua HTTP.
    ///
    /// An toàn với dữ liệu thật:
    ///  - Mỗi lần chạy dùng một CSDL tạm tên ngẫu nhiên (FoodieDB_Test_xxx), chạy xong tự xóa.
    ///  - Môi trường "Testing" => User Secrets (chứa chuỗi kết nối tới FoodieDB thật) KHÔNG được nạp.
    ///    Nếu cấu hình dưới đây bị lỗi, ứng dụng chỉ báo "thiếu cấu hình", không thể chạm vào DB thật.
    ///  - Ảnh upload ghi vào thư mục tạm, không ghi vào wwwroot của dự án.
    ///
    /// SQL Server dùng để test: mặc định .\SQLEXPRESS; đổi bằng biến môi trường FOODIE_TEST_SQLSERVER
    /// (ví dụ "(localdb)\MSSQLLocalDB").
    /// </summary>
    public class FoodieApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public const string AdminEmail = "admin@foodie.test";
        public const string AdminPassword = "Admin@Test123";

        private static string SqlServer =>
            Environment.GetEnvironmentVariable("FOODIE_TEST_SQLSERVER") is { Length: > 0 } s ? s : @".\SQLEXPRESS";

        public string DatabaseName { get; } = $"FoodieDB_Test_{Guid.NewGuid():N}";

        public string ConnectionString =>
            $"Server={SqlServer};Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";

        /// <summary>wwwroot tạm của lần chạy test (chứa uploads/ và images/products/).</summary>
        public string WebRoot { get; } = Path.Combine(Path.GetTempPath(), "foodie-tests", Guid.NewGuid().ToString("N"));

        public string UploadsFolder => Path.Combine(WebRoot, "uploads");
        public string LibraryFolder => Path.Combine(WebRoot, "images", "products");

        /// <summary>Giới hạn đăng nhập / góp ý. Bộ test thường nới rộng; RateLimitFactory dùng mức thật.</summary>
        protected virtual int AuthPerMinute => 100_000;
        protected virtual int FeedbackPer10Minutes => 100_000;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.CreateDirectory(UploadsFolder);
            Directory.CreateDirectory(LibraryFolder);

            builder.UseEnvironment("Testing");
            builder.UseSetting(WebHostDefaults.WebRootKey, WebRoot);
            builder.UseSetting("ConnectionStrings:Default", ConnectionString);
            builder.UseSetting("Jwt:Key", "khoa-ky-jwt-chi-dung-cho-kiem-thu-tu-dong-0123456789");
            builder.UseSetting("Seed:Enabled", "true");
            builder.UseSetting("Seed:AdminEmail", AdminEmail);
            builder.UseSetting("Seed:AdminPassword", AdminPassword);
            builder.UseSetting("RateLimit:AuthPerMinute", AuthPerMinute.ToString());
            builder.UseSetting("RateLimit:FeedbackPer10Minutes", FeedbackPer10Minutes.ToString());

            // Bớt log cho dễ đọc kết quả test
            builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        }

        /// <summary>Client gửi request như trình duyệt: giữ cookie, KHÔNG tự đi theo chuyển hướng (để kiểm tra 302).</summary>
        public HttpClient CreateBrowserClient() =>
            CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

        /// <summary>Mở DbContext tới CSDL test (để chuẩn bị / kiểm tra dữ liệu trực tiếp).</summary>
        public AppDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);

        public Task InitializeAsync()
        {
            // Truy cập Services để ứng dụng khởi động ngay (chạy migration + seed) trước khi các test bắt đầu
            _ = Services;
            return Task.CompletedTask;
        }

        public new async Task DisposeAsync()
        {
            await base.DisposeAsync();

            // Xóa CSDL tạm (đóng các kết nối còn trong pool trước khi DROP)
            SqlConnection.ClearAllPools();
            await using (var db = CreateDbContext())
                await db.Database.EnsureDeletedAsync();

            try { Directory.Delete(WebRoot, recursive: true); } catch (IOException) { /* bỏ qua */ }
        }
    }

    /// <summary>Dùng giới hạn thật (10 lần đăng nhập / phút, 5 góp ý / 10 phút) để kiểm thử chống spam.</summary>
    public class RateLimitFactory : FoodieApiFactory
    {
        protected override int AuthPerMinute => 10;
        protected override int FeedbackPer10Minutes => 5;
    }

    /// <summary>Các lớp test tích hợp dùng chung một ứng dụng + một CSDL tạm (chạy tuần tự trong nhóm).</summary>
    [CollectionDefinition(Name)]
    public class ApiCollection : ICollectionFixture<FoodieApiFactory>
    {
        public const string Name = "Foodie API";
    }

    [CollectionDefinition(Name)]
    public class RateLimitCollection : ICollectionFixture<RateLimitFactory>
    {
        public const string Name = "Rate limit";
    }
}
