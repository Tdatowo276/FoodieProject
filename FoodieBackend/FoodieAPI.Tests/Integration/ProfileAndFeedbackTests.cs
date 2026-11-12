using System.Net;
using FoodieAPI.DTOs;
using FoodieAPI.Tests.Infrastructure;

namespace FoodieAPI.Tests.Integration
{
    /// <summary>Hồ sơ cá nhân: họ tên, mật khẩu, ảnh đại diện, địa chỉ mặc định.</summary>
    [Collection(ApiCollection.Name)]
    public class ProfileTests(FoodieApiFactory factory)
    {
        [Fact]
        public async Task Profile_RequiresLogin()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/profile")).StatusCode);
        }

        [Fact]
        public async Task UpdateName_Validates_AndPersists()
        {
            var (client, _) = await factory.CustomerClientAsync();

            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutJsonAsync("/api/profile", new { name = " " })).StatusCode);

            var ok = await client.PutJsonAsync("/api/profile", new { name = "Nguyễn Tiến Đạt" });
            Assert.Equal("Nguyễn Tiến Đạt", (await ok.ReadAsync<ProfileDto>()).Name);
            Assert.Equal("Nguyễn Tiến Đạt", (await (await client.GetAsync("/api/auth/me")).ReadAsync<AuthUser>()).Name);
        }

        [Fact]
        public async Task ChangePassword_RequiresCurrentPassword_AndMatchingConfirmation()
        {
            var (client, auth) = await factory.CustomerClientAsync();
            Task<HttpResponseMessage> Change(string current, string next, string confirm) =>
                client.PutJsonAsync("/api/profile/password", new { currentPassword = current, newPassword = next, confirmPassword = confirm });

            var mismatch = await Change("Matkhau123", "MoiMoi123", "KhacKhac123");
            Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
            Assert.Contains("confirmPassword", await mismatch.ErrorFieldsAsync());

            var wrongCurrent = await Change("sai-roi", "MoiMoi123", "MoiMoi123");
            Assert.Equal(HttpStatusCode.BadRequest, wrongCurrent.StatusCode);
            Assert.Contains("currentPassword", await wrongCurrent.ErrorFieldsAsync());

            Assert.Equal(HttpStatusCode.BadRequest, (await Change("Matkhau123", "Matkhau123", "Matkhau123")).StatusCode);   // trùng cũ
            Assert.Equal(HttpStatusCode.NoContent, (await Change("Matkhau123", "MoiMoi123", "MoiMoi123")).StatusCode);

            var anon = factory.CreateClient();
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await anon.PostJsonAsync("/api/auth/login", new { email = auth.User.Email, password = "Matkhau123" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK,
                (await anon.PostJsonAsync("/api/auth/login", new { email = auth.User.Email, password = "MoiMoi123" })).StatusCode);
        }

        [Fact]
        public async Task Avatar_UploadReplaceRemove_CleansUpOldFiles()
        {
            var (client, _) = await factory.CustomerClientAsync();

            var first = await (await client.PostAsync("/api/profile/avatar", TestHelpers.ImageForm(TestHelpers.PngBytes, "a.png"))).ReadAsync<ProfileDto>();
            Assert.Matches(@"^/uploads/[0-9a-f]{32}\.png$", first.AvatarUrl!);

            var second = await (await client.PostAsync("/api/profile/avatar", TestHelpers.ImageForm(TestHelpers.JpgBytes, "b.jpg", "image/jpeg"))).ReadAsync<ProfileDto>();
            Assert.False(File.Exists(Path.Combine(factory.UploadsFolder, Path.GetFileName(first.AvatarUrl!))));

            var removed = await (await client.DeleteAsync("/api/profile/avatar")).ReadAsync<ProfileDto>();
            Assert.Null(removed.AvatarUrl);
            Assert.False(File.Exists(Path.Combine(factory.UploadsFolder, Path.GetFileName(second.AvatarUrl!))));

            var fake = await client.PostAsync("/api/profile/avatar", TestHelpers.ImageForm("khong-phai-anh"u8.ToArray(), "c.png"));
            Assert.Equal(HttpStatusCode.BadRequest, fake.StatusCode);
        }

        [Fact]
        public async Task DefaultAddress_SaveAndClear()
        {
            var (client, _) = await factory.CustomerClientAsync();

            var bad = await client.PutJsonAsync("/api/profile/address", new { recipientName = "Đạt", phone = "123", address = "Hà Nội" });
            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

            var saved = await (await client.PutJsonAsync("/api/profile/address",
                new { recipientName = "Nguyễn Tiến Đạt", phone = "0974679274", address = "54 ngõ 10 Láng Hạ" })).ReadAsync<ProfileDto>();
            Assert.Equal("54 ngõ 10 Láng Hạ", saved.DefaultAddress!.Address);

            var cleared = await (await client.DeleteAsync("/api/profile/address")).ReadAsync<ProfileDto>();
            Assert.Null(cleared.DefaultAddress);
        }
    }

    /// <summary>Góp ý của khách hàng + quản lý góp ý (admin).</summary>
    [Collection(ApiCollection.Name)]
    public class FeedbackTests(FoodieApiFactory factory)
    {
        private static object Body(string email, string message = "Gà rán rất ngon, giao hàng nhanh.") =>
            new { name = "Khách Góp Ý", email, topic = "food", rating = 5, message };

        [Fact]
        public async Task Anonymous_CanSendFeedback()
        {
            var response = await factory.CreateClient().PostJsonAsync("/api/feedback", Body(TestHelpers.UniqueEmail()));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.False((await response.ReadAsync<FeedbackDto>()).FromMember);
        }

        [Fact]
        public async Task Member_Feedback_IsLinkedToAccount()
        {
            var (client, _) = await factory.CustomerClientAsync();
            var response = await client.PostJsonAsync("/api/feedback", Body(TestHelpers.UniqueEmail()));
            Assert.True((await response.ReadAsync<FeedbackDto>()).FromMember);
        }

        [Fact]
        public async Task InvalidFeedback_Returns400PerField()
        {
            var response = await factory.CreateClient().PostJsonAsync("/api/feedback",
                new { name = "", email = "abc", topic = "hacked", rating = 9, message = "ngắn" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(["email", "message", "name", "rating", "topic"], (await response.ErrorFieldsAsync()).Order());
        }

        [Fact]
        public async Task Admin_CanFilterUnread_MarkRead_AndDelete()
        {
            var email = TestHelpers.UniqueEmail();
            var created = await (await factory.CreateClient().PostJsonAsync("/api/feedback", Body(email))).ReadAsync<FeedbackDto>();
            var admin = await factory.AdminClientAsync();

            var unread = await (await admin.GetAsync($"/api/admin/feedback?unread=true&keyword={email}")).ReadAsync<Paged<FeedbackDto>>();
            Assert.Single(unread.Items);

            var read = await (await admin.PatchAsync($"/api/admin/feedback/{created.Id}/read", null)).ReadAsync<FeedbackDto>();
            Assert.True(read.IsRead);
            var afterRead = await (await admin.GetAsync($"/api/admin/feedback?unread=true&keyword={email}")).ReadAsync<Paged<FeedbackDto>>();
            Assert.Empty(afterRead.Items);

            Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/feedback/{created.Id}")).StatusCode);
        }
    }
}
