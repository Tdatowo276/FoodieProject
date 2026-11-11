using Microsoft.EntityFrameworkCore;
using FoodieAPI.Models;
using FoodieAPI.Common;

namespace FoodieAPI.Data
{
    public static class Seeder
    {
        /// <summary>Tạo dữ liệu mẫu (admin, danh mục, món) khi CSDL còn trống.</summary>
        /// <param name="db">DbContext.</param>
        /// <param name="config">Cấu hình chứa Seed:AdminEmail / Seed:AdminPassword.</param>
        /// <param name="libraryImages">
        /// Ảnh trong wwwroot/images/products. Món mẫu tự được gắn ảnh nếu có file trùng tên món
        /// (ví dụ "Phở bò tái nạm" => pho-bo-tai-nam.png), nên DB tạo mới trên máy nào cũng có ảnh.
        /// </param>
        public static async Task RunAsync(AppDbContext db, IConfiguration config, IReadOnlyList<string> libraryImages)
        {
            var imageBySlug = libraryImages
                .GroupBy(url => Path.GetFileNameWithoutExtension(url))
                .ToDictionary(g => g.Key, g => g.First());

            if (!await db.Users.AnyAsync(u => u.Role == Roles.Admin))
            {
                // Mật khẩu admin không được viết cứng: lấy từ User Secrets / biến môi trường Seed__AdminPassword.
                var adminEmail = config["Seed:AdminEmail"];
                var adminPassword = config["Seed:AdminPassword"];
                if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
                    throw new InvalidOperationException(
                        "Chưa có tài khoản admin và thiếu cấu hình 'Seed:AdminEmail' / 'Seed:AdminPassword'. " +
                        "Hãy đặt bằng: dotnet user-secrets set \"Seed:AdminPassword\" \"<mật khẩu>\"");
                if (adminPassword.Length < 8)
                    throw new InvalidOperationException("Seed:AdminPassword phải dài tối thiểu 8 ký tự.");

                db.Users.Add(new User
                {
                    Username = "Quản trị viên",
                    Email = adminEmail.Trim().ToLowerInvariant(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Role = Roles.Admin
                });
            }

            if (!await db.Categories.AnyAsync())
            {
                db.Categories.AddRange(
                    new Category { Name = "Đồ ăn nhanh", Description = "Burger, pizza, gà rán và các món ăn nhanh" },
                    new Category { Name = "Món chính", Description = "Cơm, phở, mì và các món no bụng" },
                    new Category { Name = "Đồ uống", Description = "Trà, cà phê, sinh tố" },
                    new Category { Name = "Tráng miệng", Description = "Kem, bánh ngọt, flan" });
                await db.SaveChangesAsync();
            }

            if (!await db.Products.AnyAsync())
            {
                var categories = await db.Categories.ToDictionaryAsync(c => c.Name);

                var seed = new (string Name, string Category, long Price, double Rating, bool Featured, string Description)[]
                {
                    ("Burger bò phô mai", "Đồ ăn nhanh", 59000, 4.8, true, "Bò nướng than, phô mai cheddar tan chảy, rau tươi và sốt đặc biệt."),
                    ("Pizza hải sản", "Đồ ăn nhanh", 129000, 4.7, true, "Đế mỏng giòn, tôm, mực, nghêu và phô mai mozzarella."),
                    ("Gà rán giòn cay", "Đồ ăn nhanh", 75000, 4.9, true, "Gà tẩm bột chiên giòn, ướp cay vừa phải, kèm sốt mayonnaise."),
                    ("Khoai tây chiên", "Đồ ăn nhanh", 29000, 4.5, false, "Khoai cắt sợi chiên vàng, rắc muối biển, kèm tương cà."),
                    ("Cơm tấm sườn bì chả", "Món chính", 55000, 4.9, true, "Sườn nướng mật ong, bì, chả trứng và nước mắm chua ngọt."),
                    ("Phở bò tái nạm", "Món chính", 60000, 4.8, true, "Nước dùng hầm xương 12 tiếng, bánh phở mềm, thịt bò tái và nạm."),
                    ("Mì Ý sốt bò bằm", "Món chính", 69000, 4.6, false, "Mì Ý al dente, sốt cà chua bò bằm, rắc phô mai parmesan."),
                    ("Sushi tổng hợp 12 miếng", "Món chính", 159000, 4.7, false, "Cá hồi, cá ngừ, tôm và trứng cá, kèm wasabi và nước tương."),
                    ("Trà đào cam sả", "Đồ uống", 35000, 4.8, true, "Trà đen ủ lạnh, đào miếng, cam tươi và sả thơm."),
                    ("Cà phê sữa đá", "Đồ uống", 29000, 4.9, false, "Cà phê phin đậm vị pha cùng sữa đặc, đá viên."),
                    ("Sinh tố bơ", "Đồ uống", 42000, 4.7, false, "Bơ sáp xay cùng sữa tươi, béo mịn, ít ngọt."),
                    ("Kem dâu tây", "Tráng miệng", 32000, 4.5, false, "Kem tươi mát lạnh vị dâu, topping dâu tây thái lát."),
                    ("Bánh flan caramel", "Tráng miệng", 22000, 4.8, true, "Flan trứng sữa mịn như lụa, rưới caramel đắng nhẹ."),
                };

                foreach (var s in seed)
                {
                    var product = new Product
                    {
                        Category = categories[s.Category],
                        Price = s.Price,
                        Rating = s.Rating,
                        IsFeatured = s.Featured,
                        Description = s.Description
                    };
                    product.SetName(s.Name);
                    product.ImageUrl = imageBySlug.GetValueOrDefault(TextUtil.Slug(s.Name));
                    db.Products.Add(product);
                }
            }

            await db.SaveChangesAsync();
        }
    }
}