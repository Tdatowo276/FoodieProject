using System.Net;
using FoodieAPI.Tests.Infrastructure;

namespace FoodieAPI.Tests.Integration
{
    /// <summary>Khu quản trị MVC: cookie, anti-forgery (CSRF), open redirect, XSS, trang lỗi, header bảo mật, AJAX.</summary>
    [Collection(ApiCollection.Name)]
    public class AdminMvcTests(FoodieApiFactory factory)
    {
        [Fact]
        public async Task AdminArea_RedirectsAnonymousToLogin()
        {
            var response = await factory.CreateBrowserClient().GetAsync("/Admin/Products");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Admin/Account/Login?ReturnUrl=%2FAdmin%2FProducts", response.Headers.Location!.PathAndQuery);
        }

        [Fact]
        public async Task LoginForm_ContainsAntiforgeryToken_AndClientValidation()
        {
            var html = await factory.CreateBrowserClient().GetStringAsync("/Admin/Account/Login");

            Assert.NotEmpty(TestHelpers.AntiforgeryToken(html));
            Assert.Contains("data-val-required", html);
        }

        [Fact]
        public async Task Post_WithoutAntiforgeryToken_IsRejected()
        {
            var browser = factory.CreateBrowserClient();
            var response = await browser.PostAsync("/Admin/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = FoodieApiFactory.AdminEmail,
                ["Password"] = FoodieApiFactory.AdminPassword
            }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AdminLogin_SetsHttpOnlyCookie_AndOpensDashboard()
        {
            var browser = factory.CreateBrowserClient();
            var login = await browser.AdminMvcLoginAsync();

            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
            var cookie = login.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("Foodie.Admin="));
            Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/Admin/Dashboard")).StatusCode);
        }

        [Fact]
        public async Task CustomerAccount_CannotEnterAdminArea()
        {
            var email = TestHelpers.UniqueEmail();
            await factory.RegisterAsync(email, password: "Matkhau123");
            var browser = factory.CreateBrowserClient();

            var response = await browser.AdminMvcLoginAsync(email, "Matkhau123");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);                // ở lại form
            Assert.Contains("không có quyền quản trị", await response.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/Admin/Dashboard")).StatusCode);
        }

        [Fact]
        public async Task Login_IgnoresExternalReturnUrl_OpenRedirect()
        {
            var response = await factory.CreateBrowserClient().AdminMvcLoginAsync(returnUrl: "https://evil.example.com/phish");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Admin", response.Headers.Location!.OriginalString);
        }

        [Fact]
        public async Task Logout_EndsSession()
        {
            var browser = factory.CreateBrowserClient();
            await browser.AdminMvcLoginAsync();
            var page = await browser.GetStringAsync("/Admin/Dashboard");

            var logout = await browser.PostAsync("/Admin/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = TestHelpers.AntiforgeryToken(page)
            }));

            Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/Admin/Dashboard")).StatusCode);
        }

        [Fact]
        public async Task FeedbackPage_EncodesUserContent_XssSafe()
        {
            var marker = Guid.NewGuid().ToString("N");
            await factory.CreateClient().PostJsonAsync("/api/feedback", new
            {
                name = "Kẻ tấn công",
                email = $"{marker}@test.vn",
                topic = "other",
                message = $"<script>alert('{marker}')</script> nội dung"
            });
            var browser = factory.CreateBrowserClient();
            await browser.AdminMvcLoginAsync();

            var html = await browser.GetStringAsync($"/Admin/Feedback?keyword={marker}");

            Assert.Contains($"&lt;script&gt;alert(&#x27;{marker}&#x27;)&lt;/script&gt;", html);
            Assert.DoesNotContain($"<script>alert('{marker}')", html);
        }

        [Fact]
        public async Task AjaxRequest_ReturnsPartialTableOnly()
        {
            var browser = factory.CreateBrowserClient();
            await browser.AdminMvcLoginAsync();
            var request = new HttpRequestMessage(HttpMethod.Get, "/Admin/Products?keyword=burger&page=1");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var html = await (await browser.SendAsync(request)).Content.ReadAsStringAsync();

            Assert.DoesNotContain("<html", html);
            Assert.Contains("Burger", html);
        }

        [Fact]
        public async Task Errors_AreHtmlForMvc_AndProblemJsonForApi()
        {
            var browser = factory.CreateBrowserClient();
            await browser.AdminMvcLoginAsync();

            var mvc = await browser.GetAsync("/Admin/KhongTonTai");
            Assert.Equal(HttpStatusCode.NotFound, mvc.StatusCode);
            Assert.Contains("Không tìm thấy trang", await mvc.Content.ReadAsStringAsync());

            var api = await factory.CreateClient().GetAsync("/api/khong-ton-tai");
            Assert.Equal(HttpStatusCode.NotFound, api.StatusCode);
            Assert.Equal("application/problem+json", api.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task SecurityHeaders_ArePresent()
        {
            var response = await factory.CreateBrowserClient().GetAsync("/Admin/Account/Login");

            Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
            Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        }
    }

    /// <summary>Chống dò mật khẩu và spam (giới hạn thật: 10 đăng nhập / phút, 5 góp ý / 10 phút).</summary>
    [Collection(RateLimitCollection.Name)]
    public class RateLimitTests(RateLimitFactory factory)
    {
        [Fact]
        public async Task Login_11thAttemptWithinAMinute_Returns429()
        {
            var client = factory.CreateClient();
            var statuses = new List<HttpStatusCode>();
            for (var i = 0; i < 11; i++)
                statuses.Add((await client.PostJsonAsync("/api/auth/login", new { email = "x@test.vn", password = "sai" })).StatusCode);

            Assert.All(statuses.Take(10), s => Assert.Equal(HttpStatusCode.Unauthorized, s));
            Assert.Equal(HttpStatusCode.TooManyRequests, statuses[10]);
        }

        [Fact]
        public async Task Feedback_6thWithin10Minutes_Returns429()
        {
            var client = factory.CreateClient();
            var statuses = new List<HttpStatusCode>();
            for (var i = 0; i < 6; i++)
                statuses.Add((await client.PostJsonAsync("/api/feedback",
                    new { name = "Spam", email = "spam@test.vn", topic = "other", message = $"Nội dung lặp lại {i}" })).StatusCode);

            Assert.All(statuses.Take(5), s => Assert.Equal(HttpStatusCode.Created, s));
            Assert.Equal(HttpStatusCode.TooManyRequests, statuses[5]);
        }
    }
}
