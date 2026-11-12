using System.Net;
using FoodieAPI.DTOs;
using FoodieAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodieAPI.Tests.Integration
{
    /// <summary>Đặt hàng, đơn của tôi, luồng trạng thái đơn, doanh thu.</summary>
    [Collection(ApiCollection.Name)]
    public class OrderTests(FoodieApiFactory factory)
    {
        private async Task<int> ProductIdAsync(string name)
        {
            await using var db = factory.CreateDbContext();
            return await db.Products.Where(p => p.Name == name).Select(p => p.Id).SingleAsync();
        }

        private static object OrderBody(params (int ProductId, int Quantity)[] items) => new
        {
            items = items.Select(i => new { productId = i.ProductId, quantity = i.Quantity }),
            receiverName = "Nguyễn Tiến Đạt",
            phone = "0974679274",
            address = "54 ngõ 10 Láng Hạ, Hà Nội",
            paymentMethod = "cod",
            // Client gửi kèm tổng tiền giả: server phải bỏ qua và tự tính
            total = 1,
            shippingFee = 0
        };

        private async Task<(HttpClient Customer, OrderCreated Order)> PlaceOrderAsync(params (string Name, int Qty)[] items)
        {
            var (customer, _) = await factory.CustomerClientAsync();
            var lines = new List<(int, int)>();
            foreach (var (name, qty) in items) lines.Add((await ProductIdAsync(name), qty));

            var response = await customer.PostJsonAsync("/api/orders", OrderBody([.. lines]));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (customer, await response.ReadAsync<OrderCreated>());
        }

        [Fact]
        public async Task Create_UsesDatabasePrices_AndAddsShippingFee()
        {
            // Burger gà 50.000đ x 2 = 100.000đ (< 200.000đ) => + 15.000đ phí giao
            var (_, order) = await PlaceOrderAsync(("Burger gà", 2));

            Assert.Equal(115_000, order.Total);
            Assert.Equal("pending", order.Status);
            Assert.Matches(@"^FD\d{6}$", order.OrderCode);
        }

        [Fact]
        public async Task Create_FreeShipping_From200k()
        {
            // 195.000 + 15.000 = 210.000đ => miễn phí giao
            var (_, order) = await PlaceOrderAsync(("Gà sốt mật ong", 1), ("Pepsi", 1));
            Assert.Equal(210_000, order.Total);
        }

        [Fact]
        public async Task Create_WithoutLogin_Returns401()
        {
            var response = await factory.CreateClient().PostJsonAsync("/api/orders", OrderBody((1, 1)));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Create_InvalidNestedItem_ReportsExactField()
        {
            var (customer, _) = await factory.CustomerClientAsync();
            var id = await ProductIdAsync("Pepsi");

            var response = await customer.PostJsonAsync("/api/orders", OrderBody((id, 1), (id, 0)));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("items[1].quantity", await response.ErrorFieldsAsync());
        }

        [Fact]
        public async Task Create_DuplicateLinesOver99_IsRejected()
        {
            var (customer, _) = await factory.CustomerClientAsync();
            var id = await ProductIdAsync("Pepsi");

            // Mỗi dòng hợp lệ (60), nhưng gộp lại 120 phần > 99
            var response = await customer.PostJsonAsync("/api/orders", OrderBody((id, 60), (id, 60)));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Create_UnknownProduct_IsRejected()
        {
            var (customer, _) = await factory.CustomerClientAsync();
            var response = await customer.PostJsonAsync("/api/orders", OrderBody((999_999, 1)));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task OrderDetail_OnlyVisibleToOwner()
        {
            var (owner, order) = await PlaceOrderAsync(("Kem Vani", 3));
            var (stranger, _) = await factory.CustomerClientAsync();

            Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/orders/{order.OrderCode}")).StatusCode);
            // Người khác: 404 (không tiết lộ đơn có tồn tại hay không)
            Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/api/orders/{order.OrderCode}")).StatusCode);

            var mine = await (await owner.GetAsync("/api/orders/me")).ReadAsync<Paged<MyOrderDto>>();
            Assert.Single(mine.Items);
            var others = await (await stranger.GetAsync("/api/orders/me")).ReadAsync<Paged<MyOrderDto>>();
            Assert.Empty(others.Items);
        }

        [Fact]
        public async Task StatusFlow_FollowsBusinessRules()
        {
            var (customer, order) = await PlaceOrderAsync(("Cơm trắng", 2));
            var admin = await factory.AdminClientAsync();
            Task<HttpResponseMessage> Change(string status) =>
                admin.PatchJsonAsync($"/api/admin/orders/{order.OrderCode}/status", new { status });

            Assert.Equal(HttpStatusCode.BadRequest, (await Change("delivering")).StatusCode);   // nhảy cóc
            Assert.Equal(HttpStatusCode.BadRequest, (await Change("hacked")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Change("preparing")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Change("delivering")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Change("cancelled")).StatusCode);   // đang giao không hủy được

            var done = await (await Change("completed")).ReadAsync<OrderDetailDto>();
            Assert.Equal("completed", done.Status);
            Assert.Empty(done.NextStatuses);

            // Khách thấy trạng thái mới
            var mine = await (await customer.GetAsync($"/api/orders/{order.OrderCode}")).ReadAsync<OrderDetailDto>();
            Assert.Equal("completed", mine.Status);
        }

        [Fact]
        public async Task CompletingOrder_IncreasesMonthlyRevenue()
        {
            var admin = await factory.AdminClientAsync();
            async Task<long> RevenueAsync() => (await (await admin.GetAsync("/api/admin/stats")).ReadAsync<StatsDto>()).Revenue.Value;

            var before = await RevenueAsync();
            var (_, order) = await PlaceOrderAsync(("Burger bò", 4));       // 228.000đ, miễn phí giao
            foreach (var s in new[] { "preparing", "delivering", "completed" })
                await admin.PatchJsonAsync($"/api/admin/orders/{order.OrderCode}/status", new { status = s });

            Assert.Equal(before + 228_000, await RevenueAsync());
        }

        [Fact]
        public async Task ProductInHiddenCategory_CannotBeOrdered()
        {
            var admin = await factory.AdminClientAsync();
            var category = await (await admin.PostJsonAsync("/api/admin/categories", new { name = $"Ẩn {Guid.NewGuid():N}" })).ReadAsync<CategoryDto>();
            var product = await (await admin.PostJsonAsync("/api/admin/products",
                new { name = $"Món ẩn {Guid.NewGuid():N}", categoryId = category.Id, price = 10_000 })).ReadAsync<ProductDto>();
            await admin.PutJsonAsync($"/api/admin/categories/{category.Id}", new { name = category.Name, isActive = false });

            var (customer, _) = await factory.CustomerClientAsync();
            var response = await customer.PostJsonAsync("/api/orders", OrderBody((product.Id, 1)));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
