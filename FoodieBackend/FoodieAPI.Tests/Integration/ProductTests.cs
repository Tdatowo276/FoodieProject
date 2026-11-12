using System.Net;
using FoodieAPI.DTOs;
using FoodieAPI.Tests.Infrastructure;

namespace FoodieAPI.Tests.Integration
{
    /// <summary>Thực đơn công khai + quản lý món / danh mục (admin) + thư viện ảnh.</summary>
    [Collection(ApiCollection.Name)]
    public class ProductTests(FoodieApiFactory factory)
    {
        private readonly HttpClient _public = factory.CreateClient();

        private static string Unique(string name) => $"{name} {Guid.NewGuid().ToString("N")[..6]}";

        private static async Task<CategoryDto> CreateCategoryAsync(HttpClient admin, bool active = true)
        {
            var response = await admin.PostJsonAsync("/api/admin/categories", new { name = Unique("Danh mục thử"), isActive = active });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.ReadAsync<CategoryDto>();
        }

        private static async Task<ProductDto> CreateProductAsync(HttpClient admin, int categoryId, string? imageUrl = null)
        {
            var response = await admin.PostJsonAsync("/api/admin/products",
                new { name = Unique("Món thử"), categoryId, price = 45_000, imageUrl });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await response.ReadAsync<ProductDto>();
        }

        // ------------------------- Công khai -------------------------

        [Theory]
        [InlineData("burger ga")]
        [InlineData("BURGER GÀ")]
        [InlineData("burger gà")]
        public async Task Search_IgnoresCaseAndDiacritics(string keyword)
        {
            var items = await (await _public.GetAsync($"/api/products?keyword={Uri.EscapeDataString(keyword)}")).ReadAsync<List<ProductDto>>();
            Assert.Contains(items, p => p.Name == "Burger gà");
        }

        [Fact]
        public async Task List_SupportsPagingAndCategoryFilter()
        {
            var paged = await (await _public.GetAsync("/api/products?page=1&pageSize=5")).ReadAsync<Paged<ProductDto>>();
            Assert.Equal(5, paged.Items.Count);
            Assert.True(paged.Total >= 15);

            var drinks = await (await _public.GetAsync($"/api/products?category={Uri.EscapeDataString("Đồ uống")}")).ReadAsync<List<ProductDto>>();
            Assert.NotEmpty(drinks);
            Assert.All(drinks, p => Assert.Equal("Đồ uống", p.Category));
        }

        [Fact]
        public async Task Featured_ReturnsAtMost8FeaturedItems()
        {
            var featured = await (await _public.GetAsync("/api/products/featured")).ReadAsync<List<ProductDto>>();
            Assert.InRange(featured.Count, 1, 8);
            Assert.All(featured, p => Assert.True(p.IsFeatured));
        }

        [Fact]
        public async Task GetById_Unknown_Returns404ProblemJson()
        {
            var response = await _public.GetAsync("/api/products/999999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task HidingCategory_HidesItsProductsFromCustomers_ButNotFromAdmin()
        {
            var admin = await factory.AdminClientAsync();
            var category = await CreateCategoryAsync(admin);
            var product = await CreateProductAsync(admin, category.Id);
            Assert.Equal(HttpStatusCode.OK, (await _public.GetAsync($"/api/products/{product.Id}")).StatusCode);

            await admin.PutJsonAsync($"/api/admin/categories/{category.Id}", new { name = category.Name, isActive = false });

            Assert.Equal(HttpStatusCode.NotFound, (await _public.GetAsync($"/api/products/{product.Id}")).StatusCode);
            var publicCategories = await (await _public.GetAsync("/api/categories")).ReadAsync<List<CategoryDto>>();
            Assert.DoesNotContain(publicCategories, c => c.Id == category.Id);
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/admin/products/{product.Id}")).StatusCode);
        }

        // ------------------------- Quản trị: danh mục -------------------------

        [Fact]
        public async Task Category_DuplicateName_IgnoringCase_Returns400()
        {
            var admin = await factory.AdminClientAsync();
            var response = await admin.PostJsonAsync("/api/admin/categories", new { name = "ĐỒ UỐNG" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("name", await response.ErrorFieldsAsync());
        }

        [Fact]
        public async Task Category_WithProducts_CannotBeDeleted_EmptyOneCan()
        {
            var admin = await factory.AdminClientAsync();
            var used = await CreateCategoryAsync(admin);
            await CreateProductAsync(admin, used.Id);
            var empty = await CreateCategoryAsync(admin);

            Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/admin/categories/{used.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/categories/{empty.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/admin/categories/{empty.Id}")).StatusCode);
        }

        // ------------------------- Quản trị: món ăn -------------------------

        [Fact]
        public async Task Product_CreateUpdateDelete()
        {
            var admin = await factory.AdminClientAsync();
            var category = await CreateCategoryAsync(admin);
            var product = await CreateProductAsync(admin, category.Id);

            var updated = await admin.PutJsonAsync($"/api/admin/products/{product.Id}",
                new { name = product.Name, categoryId = category.Id, price = 60_000, isFeatured = true });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            var dto = await updated.ReadAsync<ProductDto>();
            Assert.Equal(60_000, dto.Price);
            Assert.True(dto.IsFeatured);

            Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/products/{product.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await _public.GetAsync($"/api/products/{product.Id}")).StatusCode);
        }

        [Fact]
        public async Task Product_InvalidData_Returns400PerField()
        {
            var admin = await factory.AdminClientAsync();
            var response = await admin.PostJsonAsync("/api/admin/products", new { name = "", price = 5, imageUrl = "https://evil.example.com/a.png" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(["categoryId", "imageUrl", "name", "price"], (await response.ErrorFieldsAsync()).Order());
        }

        [Fact]
        public async Task Product_UnknownCategory_Returns400()
        {
            var admin = await factory.AdminClientAsync();
            var response = await admin.PostJsonAsync("/api/admin/products", new { name = "Món thử", categoryId = 999_999, price = 30_000 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("categoryId", await response.ErrorFieldsAsync());
        }

        [Fact]
        public async Task LibraryImage_MustExistOnDisk_AndIsNeverDeleted()
        {
            var admin = await factory.AdminClientAsync();
            var category = await CreateCategoryAsync(admin);
            var fileName = $"mon-thu-{Guid.NewGuid():N}.png";
            await File.WriteAllBytesAsync(Path.Combine(factory.LibraryFolder, fileName), TestHelpers.PngBytes);

            // Có trong danh sách thư viện ảnh
            var library = await (await admin.GetAsync("/api/admin/images")).ReadAsync<List<string>>();
            Assert.Contains($"/images/products/{fileName}", library);

            // File không tồn tại => 400; file có thật => 201
            var missing = await admin.PostJsonAsync("/api/admin/products",
                new { name = "Món thử", categoryId = category.Id, price = 30_000, imageUrl = "/images/products/khong-co-file.png" });
            Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

            var product = await CreateProductAsync(admin, category.Id, $"/images/products/{fileName}");
            Assert.Equal($"/images/products/{fileName}", product.ImageUrl);

            // Xóa món: ảnh thư viện dùng chung của nhóm vẫn còn
            await admin.DeleteAsync($"/api/admin/products/{product.Id}");
            Assert.True(File.Exists(Path.Combine(factory.LibraryFolder, fileName)));
        }
    }
}
