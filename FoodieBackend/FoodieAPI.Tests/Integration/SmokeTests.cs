using System.Net;
using FoodieAPI.DTOs;
using FoodieAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FoodieAPI.Tests.Integration
{
    /// <summary>
    /// Ứng dụng khởi động được trên CSDL tạm: migration + dữ liệu mẫu.
    /// Các lớp test dùng chung CSDL và có thể thêm dữ liệu, nên kiểm tra theo TÊN dữ liệu mẫu thay vì đếm tổng.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class SmokeTests(FoodieApiFactory factory)
    {
        private static readonly string[] SeedCategories =
            ["Đồ ăn nhanh", "Món chính", "Đồ uống", "Tráng miệng", "Combo Siêu Ưu Đãi", "Đồ ăn kèm"];

        private static readonly string[] SeedProducts =
        [
            "Mỳ ý sốt cay vừa", "Salad rau củ", "Viên phô mai", "Gà Chiên Giòn", "Burger bò", "Burger gà", "Gà sốt mật ong",
            "Trà Chanh Lipton", "Pepsi", "Mirinda", "Kem Vani", "Gà giòn nguyên con", "Combo Burger Gà Giòn", "Combo Burger", "Cơm trắng"
        ];

        [Fact]
        public async Task App_UsesTemporaryDatabase_NotRealOne()
        {
            await using var db = factory.CreateDbContext();
            Assert.StartsWith("FoodieDB_Test_", db.Database.GetDbConnection().Database);
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());     // đủ migration
        }

        [Fact]
        public async Task Seeder_CreatesAdminCategoriesAndMenu()
        {
            await using var db = factory.CreateDbContext();
            Assert.True(await db.Users.AnyAsync(u => u.Email == FoodieApiFactory.AdminEmail && u.Role == "Admin"));
            Assert.Equal(SeedCategories.Order(), await db.Categories.Where(c => SeedCategories.Contains(c.Name)).Select(c => c.Name).OrderBy(n => n).ToListAsync());
            Assert.Equal(SeedProducts.Length, await db.Products.CountAsync(p => SeedProducts.Contains(p.Name)));
        }

        [Fact]
        public async Task PublicMenu_ContainsSeededMenu()
        {
            var response = await factory.CreateClient().GetAsync("/api/products");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var names = (await response.ReadAsync<List<ProductDto>>()).Select(p => p.Name).ToHashSet();
            Assert.All(SeedProducts, name => Assert.Contains(name, names));
        }
    }
}
