using System.Net;
using FoodieAPI.DTOs;
using FoodieAPI.Tests.Infrastructure;

namespace FoodieAPI.Tests.Integration
{
    /// <summary>Upload ảnh an toàn: kiểm tra đuôi file, dung lượng, nội dung thật, quyền.</summary>
    [Collection(ApiCollection.Name)]
    public class UploadTests(FoodieApiFactory factory)
    {
        private const string Url = "/api/admin/uploads";

        [Fact]
        public async Task ValidPng_IsSaved_WithServerGeneratedName()
        {
            var admin = await factory.AdminClientAsync();
            var response = await admin.PostAsync(Url, TestHelpers.ImageForm(TestHelpers.PngBytes, "../../anh cua toi.png"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var url = (await response.ReadAsync<UploadResult>()).Url;
            // Tên do server sinh (GUID), không dùng tên/đường dẫn client gửi => chặn path traversal
            Assert.Matches(@"^/uploads/[0-9a-f]{32}\.png$", url);
            Assert.True(File.Exists(Path.Combine(factory.UploadsFolder, Path.GetFileName(url))));
        }

        [Fact]
        public async Task ValidJpg_IsAccepted()
        {
            var admin = await factory.AdminClientAsync();
            var response = await admin.PostAsync(Url, TestHelpers.ImageForm(TestHelpers.JpgBytes, "mon.jpeg", "image/jpeg"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("shell.php", "image/png")]      // sai đuôi file
        [InlineData("anh.gif", "image/gif")]
        [InlineData("anh.svg", "image/svg+xml")]
        public async Task WrongExtension_IsRejected(string fileName, string contentType)
        {
            var admin = await factory.AdminClientAsync();
            var response = await admin.PostAsync(Url, TestHelpers.ImageForm(TestHelpers.PngBytes, fileName, contentType));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task FakeContent_WithImageExtension_IsRejected()
        {
            var admin = await factory.AdminClientAsync();
            var before = Directory.GetFiles(factory.UploadsFolder).Length;

            var response = await admin.PostAsync(Url, TestHelpers.ImageForm("<?php system($_GET['c']); ?>"u8.ToArray(), "anh.png"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(before, Directory.GetFiles(factory.UploadsFolder).Length);   // không lưu file rác
        }

        [Fact]
        public async Task ExtensionNotMatchingContent_IsRejected()
        {
            var admin = await factory.AdminClientAsync();
            // Nội dung JPG nhưng đặt đuôi .png
            var response = await admin.PostAsync(Url, TestHelpers.ImageForm(TestHelpers.JpgBytes, "anh.png"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task FileOver2MB_IsRejected()
        {
            var admin = await factory.AdminClientAsync();
            var big = new byte[2 * 1024 * 1024 + 1];
            TestHelpers.PngBytes.CopyTo(big, 0);

            var response = await admin.PostAsync(Url, TestHelpers.ImageForm(big, "lon.png"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Customer_CannotUpload()
        {
            var (customer, _) = await factory.CustomerClientAsync();
            var response = await customer.PostAsync(Url, TestHelpers.ImageForm(TestHelpers.PngBytes, "anh.png"));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ReplacingProductImage_DeletesOldUploadedFile()
        {
            var admin = await factory.AdminClientAsync();
            async Task<string> UploadAsync() =>
                (await (await admin.PostAsync(Url, TestHelpers.ImageForm(TestHelpers.PngBytes, "a.png"))).ReadAsync<UploadResult>()).Url;

            var first = await UploadAsync();
            var created = await admin.PostJsonAsync("/api/admin/products",
                new { name = $"Món ảnh {Guid.NewGuid():N}", categoryId = 1, price = 20_000, imageUrl = first });
            var product = await created.ReadAsync<ProductDto>();

            var second = await UploadAsync();
            await admin.PutJsonAsync($"/api/admin/products/{product.Id}",
                new { name = product.Name, categoryId = 1, price = 20_000, imageUrl = second });

            Assert.False(File.Exists(Path.Combine(factory.UploadsFolder, Path.GetFileName(first))));
            Assert.True(File.Exists(Path.Combine(factory.UploadsFolder, Path.GetFileName(second))));
        }
    }
}
