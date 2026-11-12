using System.Net;
using FoodieAPI.Tests.Infrastructure;

namespace FoodieAPI.Tests.Integration
{
    /// <summary>Phân quyền phía server: 2 vai trò, 2 cơ chế xác thực tách biệt, khóa tài khoản.</summary>
    [Collection(ApiCollection.Name)]
    public class AuthorizationTests(FoodieApiFactory factory)
    {
        [Theory]
        [InlineData("/api/admin/stats")]
        [InlineData("/api/admin/orders")]
        [InlineData("/api/admin/products")]
        [InlineData("/api/admin/users")]
        [InlineData("/api/admin/feedback")]
        public async Task AdminApi_Anonymous401_Customer403_Admin200(string url)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync(url)).StatusCode);

            var (customer, _) = await factory.CustomerClientAsync();
            Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync(url)).StatusCode);

            var admin = await factory.AdminClientAsync();
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(url)).StatusCode);
        }

        [Fact]
        public async Task AdminCookie_IsNotAccepted_ByApi()
        {
            // Cookie đăng nhập khu Admin MVC không có hiệu lực với /api (API chỉ nhận JWT) => không bị CSRF qua cookie
            var browser = factory.CreateBrowserClient();
            await browser.AdminMvcLoginAsync();

            Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/Admin/Dashboard")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/admin/stats")).StatusCode);
        }

        [Fact]
        public async Task AdminJwt_IsNotAccepted_ByMvcArea()
        {
            var admin = await factory.AdminClientAsync();
            var browser = factory.CreateBrowserClient();
            browser.DefaultRequestHeaders.Authorization = admin.DefaultRequestHeaders.Authorization;

            var response = await browser.GetAsync("/Admin/Dashboard");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith("/Admin/Account/Login", response.Headers.Location!.PathAndQuery);
        }

        [Fact]
        public async Task LockingAccount_RevokesExistingToken_Immediately()
        {
            var (customer, auth) = await factory.CustomerClientAsync();
            var admin = await factory.AdminClientAsync();
            Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/api/auth/me")).StatusCode);

            var locked = await admin.PatchJsonAsync($"/api/admin/users/{auth.User.Id}/lock", new { locked = true });
            Assert.Equal(HttpStatusCode.OK, locked.StatusCode);

            // Token cũ vẫn còn hạn nhưng bị từ chối ngay
            Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync("/api/auth/me")).StatusCode);
            // Đăng nhập lại: đúng mật khẩu nhưng bị khóa => 403
            var login = await factory.CreateClient().PostJsonAsync("/api/auth/login", new { email = auth.User.Email, password = "Matkhau123" });
            Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);

            await admin.PatchJsonAsync($"/api/admin/users/{auth.User.Id}/lock", new { locked = false });
            Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/api/auth/me")).StatusCode);
        }

        [Fact]
        public async Task AdminAccount_CannotBeLocked()
        {
            var admin = await factory.AdminClientAsync();
            var me = await (await admin.GetAsync("/api/auth/me")).ReadJsonAsync();

            var response = await admin.PatchJsonAsync($"/api/admin/users/{me.GetProperty("id").GetInt32()}/lock", new { locked = true });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
