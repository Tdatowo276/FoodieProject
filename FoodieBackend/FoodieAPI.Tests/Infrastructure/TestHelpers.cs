using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using FoodieAPI.DTOs;

namespace FoodieAPI.Tests.Infrastructure
{
    /// <summary>Tiện ích gọi API trong test.</summary>
    public static partial class TestHelpers
    {
        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        // Ảnh hợp lệ tối thiểu: chỉ cần đúng "magic bytes" đầu file là server nhận là PNG / JPG
        public static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52];
        public static readonly byte[] JpgBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46, 0x49, 0x46];

        public static string UniqueEmail() => $"u{Guid.NewGuid():N}@test.vn";

        public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
            (await response.Content.ReadFromJsonAsync<T>(Json))!;

        public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
            await response.Content.ReadFromJsonAsync<JsonElement>(Json);

        /// <summary>Các trường bị lỗi trong ValidationProblemDetails (ví dụ "items[0].quantity").</summary>
        public static async Task<string[]> ErrorFieldsAsync(this HttpResponseMessage response)
        {
            var json = await response.ReadJsonAsync();
            return json.TryGetProperty("errors", out var errors)
                ? errors.EnumerateObject().Select(p => p.Name).ToArray()
                : [];
        }

        public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string url, object body) =>
            client.PostAsJsonAsync(url, body, Json);

        public static Task<HttpResponseMessage> PutJsonAsync(this HttpClient client, string url, object body) =>
            client.PutAsJsonAsync(url, body, Json);

        public static Task<HttpResponseMessage> PatchJsonAsync(this HttpClient client, string url, object body) =>
            client.PatchAsJsonAsync(url, body, Json);

        public static HttpClient WithToken(this HttpClient client, string token)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        /// <summary>Đăng ký một khách hàng mới; trả về token + thông tin.</summary>
        public static async Task<AuthResult> RegisterAsync(this FoodieApiFactory factory,
            string? email = null, string password = "Matkhau123", string name = "Khách Kiểm Thử")
        {
            var response = await factory.CreateClient().PostJsonAsync("/api/auth/register",
                new { name, email = email ?? UniqueEmail(), password });
            response.EnsureSuccessStatusCode();
            return await response.ReadAsync<AuthResult>();
        }

        /// <summary>Client đã đăng nhập bằng một tài khoản khách hàng mới.</summary>
        public static async Task<(HttpClient Client, AuthResult Auth)> CustomerClientAsync(this FoodieApiFactory factory)
        {
            var auth = await factory.RegisterAsync();
            return (factory.CreateClient().WithToken(auth.Token), auth);
        }

        /// <summary>Client đã đăng nhập bằng tài khoản admin (JWT).</summary>
        public static async Task<HttpClient> AdminClientAsync(this FoodieApiFactory factory)
        {
            var response = await factory.CreateClient().PostJsonAsync("/api/auth/login",
                new { email = FoodieApiFactory.AdminEmail, password = FoodieApiFactory.AdminPassword });
            response.EnsureSuccessStatusCode();
            var auth = await response.ReadAsync<AuthResult>();
            return factory.CreateClient().WithToken(auth.Token);
        }

        public static MultipartFormDataContent ImageForm(byte[] bytes, string fileName, string contentType = "image/png")
        {
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            return new MultipartFormDataContent { { file, "file", fileName } };
        }

        // ---------------------- MVC (khu Admin, cookie + anti-forgery) ----------------------

        [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
        private static partial Regex AntiforgeryRegex();

        /// <summary>Lấy anti-forgery token trong form của trang HTML.</summary>
        public static string AntiforgeryToken(string html) =>
            AntiforgeryRegex().Match(html) is { Success: true } m ? m.Groups[1].Value
                : throw new InvalidOperationException("Trang không có anti-forgery token.");

        /// <summary>Đăng nhập khu Admin MVC (cookie). Trả về response của POST đăng nhập.</summary>
        public static async Task<HttpResponseMessage> AdminMvcLoginAsync(this HttpClient browser,
            string email = FoodieApiFactory.AdminEmail, string password = FoodieApiFactory.AdminPassword, string? returnUrl = null)
        {
            var page = await browser.GetStringAsync("/Admin/Account/Login");
            return await browser.PostAsync("/Admin/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = password,
                ["ReturnUrl"] = returnUrl ?? "",
                ["__RequestVerificationToken"] = AntiforgeryToken(page)
            }));
        }
    }
}
