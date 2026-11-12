using System.Net;
using FoodieAPI.DTOs;
using FoodieAPI.Tests.Infrastructure;

namespace FoodieAPI.Tests.Integration
{
    /// <summary>Đánh giá món ăn (chỉ người đã mua) và món bán chạy.</summary>
    [Collection(ApiCollection.Name)]
    public class ReviewTests(FoodieApiFactory factory)
    {
        /// <summary>Tạo một món mới riêng cho test (để số liệu không lẫn với test khác).</summary>
        private async Task<ProductDto> NewProductAsync(HttpClient admin, long price = 20_000)
        {
            var response = await admin.PostJsonAsync("/api/admin/products",
                new { name = $"Món đánh giá {Guid.NewGuid():N}", categoryId = 1, price });
            return await response.ReadAsync<ProductDto>();
        }

        /// <summary>Khách đặt món rồi admin đưa đơn tới trạng thái <paramref name="finalStatus"/>.</summary>
        private static async Task<string> OrderAsync(HttpClient customer, HttpClient admin, int productId, int quantity, string finalStatus)
        {
            var created = await customer.PostJsonAsync("/api/orders", new
            {
                items = new[] { new { productId, quantity } },
                receiverName = "Khách Đánh Giá",
                phone = "0901234567",
                address = "1 Lê Lợi",
                paymentMethod = "cod"
            });
            var code = (await created.ReadAsync<OrderCreated>()).OrderCode;

            var flow = finalStatus == "cancelled" ? new[] { "cancelled" } : ["preparing", "delivering", "completed"];
            foreach (var status in flow)
            {
                await admin.PatchJsonAsync($"/api/admin/orders/{code}/status", new { status });
                if (status == finalStatus) break;
            }
            return code;
        }

        private static Task<HttpResponseMessage> ReviewAsync(HttpClient client, int productId, int? rating, string? comment = null) =>
            client.PutJsonAsync($"/api/products/{productId}/reviews/me", new { rating, comment });

        [Fact]
        public async Task CannotReview_WithoutPurchase_OrBeforeOrderCompleted()
        {
            var admin = await factory.AdminClientAsync();
            var product = await NewProductAsync(admin);
            var (customer, _) = await factory.CustomerClientAsync();

            // Chưa mua
            Assert.Equal(HttpStatusCode.Forbidden, (await ReviewAsync(customer, product.Id, 5)).StatusCode);
            var me = await (await customer.GetAsync($"/api/products/{product.Id}/reviews/me")).ReadAsync<MyReviewDto>();
            Assert.False(me.CanReview);

            // Đã đặt nhưng đơn mới "đang giao" (chưa nhận hàng)
            await OrderAsync(customer, admin, product.Id, 1, "delivering");
            Assert.Equal(HttpStatusCode.Forbidden, (await ReviewAsync(customer, product.Id, 5)).StatusCode);

            // Đơn bị hủy cũng không tính
            var (other, _) = await factory.CustomerClientAsync();
            await OrderAsync(other, admin, product.Id, 1, "cancelled");
            Assert.Equal(HttpStatusCode.Forbidden, (await ReviewAsync(other, product.Id, 5)).StatusCode);
        }

        [Fact]
        public async Task AfterCompletedOrder_CanReview_OncePerProduct_AndAverageIsUpdated()
        {
            var admin = await factory.AdminClientAsync();
            var product = await NewProductAsync(admin);
            var (alice, _) = await factory.CustomerClientAsync();
            var (bob, _) = await factory.CustomerClientAsync();
            await OrderAsync(alice, admin, product.Id, 1, "completed");
            await OrderAsync(bob, admin, product.Id, 1, "completed");

            Assert.True((await (await alice.GetAsync($"/api/products/{product.Id}/reviews/me")).ReadAsync<MyReviewDto>()).CanReview);
            Assert.Equal(HttpStatusCode.OK, (await ReviewAsync(alice, product.Id, 5, "Rất ngon!")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await ReviewAsync(bob, product.Id, 2)).StatusCode);

            // Gửi lần 2 = sửa đánh giá cũ, không tạo thêm
            Assert.Equal(HttpStatusCode.OK, (await ReviewAsync(bob, product.Id, 4, "Đã đổi ý")).StatusCode);

            var reviews = await (await factory.CreateClient().GetAsync($"/api/products/{product.Id}/reviews")).ReadAsync<ProductReviewsDto>();
            Assert.Equal(2, reviews.Summary.Count);
            Assert.Equal(4.5, reviews.Summary.Average);                 // (5 + 4) / 2
            Assert.Equal([0, 0, 0, 1, 1], reviews.Summary.Stars);       // 1 lượt 4 sao, 1 lượt 5 sao

            // Điểm lưu sẵn trên món cũng được cập nhật
            var updated = await (await factory.CreateClient().GetAsync($"/api/products/{product.Id}")).ReadAsync<ProductDto>();
            Assert.Equal(4.5, updated.Rating);
            Assert.Equal(2, updated.ReviewCount);
        }

        [Fact]
        public async Task InvalidRating_Returns400()
        {
            var admin = await factory.AdminClientAsync();
            var product = await NewProductAsync(admin);
            var (customer, _) = await factory.CustomerClientAsync();
            await OrderAsync(customer, admin, product.Id, 1, "completed");

            var response = await ReviewAsync(customer, product.Id, 6, new string('a', 501));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(["comment", "rating"], (await response.ErrorFieldsAsync()).Order());
        }

        [Fact]
        public async Task OwnerAndAdmin_CanDeleteReview_RatingIsRecalculated()
        {
            var admin = await factory.AdminClientAsync();
            var product = await NewProductAsync(admin);
            var (alice, _) = await factory.CustomerClientAsync();
            var (bob, _) = await factory.CustomerClientAsync();
            await OrderAsync(alice, admin, product.Id, 1, "completed");
            await OrderAsync(bob, admin, product.Id, 1, "completed");
            await ReviewAsync(alice, product.Id, 5);
            var bobReview = await (await ReviewAsync(bob, product.Id, 1, "spam")).ReadAsync<ReviewDto>();

            // Admin xóa đánh giá không phù hợp => điểm còn lại 5
            Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/reviews/{bobReview.Id}")).StatusCode);
            Assert.Equal(5, (await (await factory.CreateClient().GetAsync($"/api/products/{product.Id}")).ReadAsync<ProductDto>()).Rating);

            // Chủ đánh giá tự xóa => món về "chưa có đánh giá"
            Assert.Equal(HttpStatusCode.NoContent, (await alice.DeleteAsync($"/api/products/{product.Id}/reviews/me")).StatusCode);
            var after = await (await factory.CreateClient().GetAsync($"/api/products/{product.Id}")).ReadAsync<ProductDto>();
            Assert.Equal(0, after.Rating);
            Assert.Equal(0, after.ReviewCount);
        }

        [Fact]
        public async Task Customer_CannotUseAdminReviewApi()
        {
            var (customer, _) = await factory.CustomerClientAsync();
            Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/api/admin/reviews")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await customer.DeleteAsync("/api/admin/reviews/1")).StatusCode);
        }

        [Fact]
        public async Task TopProducts_CountOnlyCompletedOrders()
        {
            var admin = await factory.AdminClientAsync();
            var hit = await NewProductAsync(admin, price: 10_000);
            var (customer, _) = await factory.CustomerClientAsync();

            await OrderAsync(customer, admin, hit.Id, 99, "completed");   // 99 phần => đứng đầu
            await OrderAsync(customer, admin, hit.Id, 50, "cancelled");   // đơn hủy: không tính
            await OrderAsync(customer, admin, hit.Id, 30, "delivering");  // chưa hoàn thành: không tính

            var top = await (await admin.GetAsync("/api/admin/stats/top-products?take=5")).ReadAsync<List<TopProductDto>>();
            var first = top[0];
            Assert.Equal(hit.Id, first.ProductId);
            Assert.Equal(99, first.Quantity);
            Assert.Equal(990_000, first.Revenue);

            // Trang chủ khách hàng cũng thấy món bán chạy
            var bestSellers = await (await factory.CreateClient().GetAsync("/api/products/best-sellers")).ReadAsync<List<TopProductDto>>();
            Assert.Equal(hit.Id, bestSellers[0].ProductId);
        }

        [Fact]
        public async Task AdminMvc_DashboardShowsTopProducts_AndReviewsPageEncodesComments()
        {
            var admin = await factory.AdminClientAsync();
            var product = await NewProductAsync(admin);
            var (customer, _) = await factory.CustomerClientAsync();
            await OrderAsync(customer, admin, product.Id, 3, "completed");
            var marker = Guid.NewGuid().ToString("N");
            await ReviewAsync(customer, product.Id, 1, $"<script>alert('{marker}')</script>");

            var browser = factory.CreateBrowserClient();
            await browser.AdminMvcLoginAsync();

            var dashboard = await browser.GetStringAsync("/Admin/Dashboard");
            Assert.Contains("Món bán chạy", dashboard);
            Assert.Contains(product.Name, dashboard);

            var reviewsPage = await browser.GetStringAsync($"/Admin/Reviews?keyword={marker}");
            Assert.Contains("&lt;script&gt;", reviewsPage);                    // chống XSS
            Assert.DoesNotContain($"<script>alert('{marker}')", reviewsPage);
        }

        [Fact]
        public async Task ReviewsOfHiddenProduct_AreNotPublic()
        {
            var admin = await factory.AdminClientAsync();
            var category = await (await admin.PostJsonAsync("/api/admin/categories", new { name = $"Ẩn {Guid.NewGuid():N}" })).ReadAsync<CategoryDto>();
            var product = await (await admin.PostJsonAsync("/api/admin/products",
                new { name = $"Món ẩn {Guid.NewGuid():N}", categoryId = category.Id, price = 10_000 })).ReadAsync<ProductDto>();
            await admin.PutJsonAsync($"/api/admin/categories/{category.Id}", new { name = category.Name, isActive = false });

            Assert.Equal(HttpStatusCode.NotFound, (await factory.CreateClient().GetAsync($"/api/products/{product.Id}/reviews")).StatusCode);
        }
    }
}
