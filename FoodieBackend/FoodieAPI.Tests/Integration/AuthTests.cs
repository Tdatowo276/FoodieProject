using System.Net;
using FoodieAPI.DTOs;
using FoodieAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodieAPI.Tests.Integration
{
    /// <summary>Đăng ký / đăng nhập / JWT.</summary>
    [Collection(ApiCollection.Name)]
    public class AuthTests(FoodieApiFactory factory)
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task Register_ReturnsToken_WithCustomerRole()
        {
            var auth = await factory.RegisterAsync(name: "Trương Thị Luyến");

            Assert.False(string.IsNullOrEmpty(auth.Token));
            Assert.Equal("Customer", auth.User.Role);
            Assert.Equal("Trương Thị Luyến", auth.User.Name);
        }

        [Fact]
        public async Task Register_CannotChooseOwnRole()
        {
            // Client cố gửi thêm "role": "Admin" => server bỏ qua, luôn gán Customer
            var response = await _client.PostJsonAsync("/api/auth/register",
                new { name = "Kẻ gian", email = TestHelpers.UniqueEmail(), password = "Matkhau123", role = "Admin" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Customer", (await response.ReadAsync<AuthResult>()).User.Role);
        }

        [Fact]
        public async Task Register_DuplicateEmail_IgnoringCase_Returns409()
        {
            var email = TestHelpers.UniqueEmail();
            await factory.RegisterAsync(email);

            var response = await _client.PostJsonAsync("/api/auth/register",
                new { name = "Người thứ hai", email = email.ToUpperInvariant(), password = "Matkhau123" });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Register_InvalidData_Returns400WithFieldErrors()
        {
            var response = await _client.PostJsonAsync("/api/auth/register", new { name = "A", email = "sai-email", password = "1" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(["email", "name", "password"], (await response.ErrorFieldsAsync()).Order());
        }

        [Fact]
        public async Task Password_IsStoredAsBCryptHash_NotPlainText()
        {
            var email = TestHelpers.UniqueEmail();
            await factory.RegisterAsync(email, password: "Matkhau123");

            await using var db = factory.CreateDbContext();
            var hash = await db.Users.Where(u => u.Email == email).Select(u => u.PasswordHash).SingleAsync();
            Assert.DoesNotContain("Matkhau123", hash);
            Assert.StartsWith("$2", hash);                               // định dạng BCrypt
            Assert.True(BCrypt.Net.BCrypt.Verify("Matkhau123", hash));
        }

        [Fact]
        public async Task Login_Succeeds_WithCorrectPassword()
        {
            var email = TestHelpers.UniqueEmail();
            await factory.RegisterAsync(email, password: "Matkhau123");

            var response = await _client.PostJsonAsync("/api/auth/login", new { email, password = "Matkhau123" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Login_WrongPasswordAndUnknownEmail_GiveSameError()
        {
            var email = TestHelpers.UniqueEmail();
            await factory.RegisterAsync(email);

            var wrongPassword = await _client.PostJsonAsync("/api/auth/login", new { email, password = "sai-mat-khau" });
            var unknownEmail = await _client.PostJsonAsync("/api/auth/login", new { email = TestHelpers.UniqueEmail(), password = "sai-mat-khau" });

            Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
            // Cùng thông báo => không dò được email nào đã đăng ký
            Assert.Equal((await wrongPassword.ReadJsonAsync()).GetProperty("title").GetString(),
                         (await unknownEmail.ReadJsonAsync()).GetProperty("title").GetString());
        }

        [Fact]
        public async Task Me_RequiresValidToken()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/auth/me")).StatusCode);

            var forged = factory.CreateClient().WithToken("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIiwicm9sZSI6IkFkbWluIn0.gia-mao");
            Assert.Equal(HttpStatusCode.Unauthorized, (await forged.GetAsync("/api/auth/me")).StatusCode);

            var (client, auth) = await factory.CustomerClientAsync();
            var me = await (await client.GetAsync("/api/auth/me")).ReadAsync<AuthUser>();
            Assert.Equal(auth.User.Email, me.Email);
        }
    }
}
