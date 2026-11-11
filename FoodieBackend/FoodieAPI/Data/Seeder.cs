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

            // Thực đơn mẫu = thực đơn thật của nhóm (xuất từ CSDL ngày 12/11/2026).
            // Thứ tự danh mục quyết định thứ tự tab trên trang chủ.
            if (!await db.Categories.AnyAsync())
            {
                db.Categories.AddRange(
                    new Category { Name = "Đồ ăn nhanh", Description = "Burger, pizza, gà rán và các món ăn nhanh" },
                    new Category { Name = "Món chính", Description = "Cơm, phở, mì và các món no bụng" },
                    new Category { Name = "Đồ uống" },
                    new Category { Name = "Tráng miệng", Description = "Kem, bánh ngọt, flan" },
                    new Category { Name = "Combo Siêu Ưu Đãi" },
                    new Category { Name = "Đồ ăn kèm" });
                await db.SaveChangesAsync();
            }

            if (!await db.Products.AnyAsync())
            {
                var categories = await db.Categories.ToDictionaryAsync(c => c.Name);

                // Image: tên file trong wwwroot/images/products (thiếu file thì món chưa có ảnh, không lỗi)
                var seed = new (string Name, string Category, long Price, double Rating, bool Featured, string Image, string Description)[]
                {
                    ("Mỳ ý sốt cay vừa", "Món chính", 49000, 4.7, true, "my-y-bo-bam.png", "Chưa có mô tả cho sản phẩm này"),
                    ("Salad rau củ", "Đồ ăn nhanh", 25000, 4.9, false, "salad-rau-cu.png", "Chưa có mô tả cho sản phẩm này."),
                    ("Viên phô mai", "Đồ ăn nhanh", 32000, 4.5, false, "vien-pho-mai.png", "Viên Phô Mai Hoàng Kim: 3 viên"),
                    ("Gà Chiên Giòn", "Món chính", 38000, 4.9, true, "ga-ran-thuong.png", "Miếng gà chiên giòn (miếng ngẫu nhiên)"),
                    ("Burger bò", "Món chính", 57000, 4.8, true, "burger-bo.png", "01 Burger bò"),
                    ("Burger gà", "Món chính", 50000, 4.6, true, "burger-ga.png", "01 Burger gà cay / không cay"),
                    ("Gà sốt mật ong", "Món chính", 195000, 4.7, true, "ga-ran-sot-mat-ong.png", "01 Gà sốt mật ong nguyên con - 1/2 Gà sốt mật ong nguyên con"),
                    ("Trà Chanh Lipton", "Đồ uống", 29000, 4.8, false, "tra-lipton.png", "Trà chanh Lipton siu ngon."),
                    ("Pepsi", "Đồ uống", 15000, 4.9, false, "pepsi.png", "Đồ uống nhanh."),
                    ("Mirinda", "Đồ uống", 15000, 4.7, false, "mirinda.png", "Đồ uống nhanh."),
                    ("Kem Vani", "Tráng miệng", 10000, 4.5, false, "kem-vani.png", "Kem tươi mát lạnh vị Vani siêu ngon."),
                    ("Gà giòn nguyên con", "Món chính", 195000, 4.8, true, "ga-ran-nua-con.png", "01 Gà giòn nguyên con - 1/2 Gà giòn nguyên con"),
                    ("Combo Burger Gà Giòn", "Combo Siêu Ưu Đãi", 59000, 0, false, "combo-burger-ga-them-pepsi.png", "1 Burger gà giòn + 1 Miếng gà chiên giòn + 1 Pepsi"),
                    ("Combo Burger", "Combo Siêu Ưu Đãi", 69000, 0, true, "combo-2-burger-them-pepsi.png", "Combo bao gồm: - 2 Burger gà giòn  - 1 Pepsi"),
                    ("Cơm trắng", "Đồ ăn kèm", 5000, 0, false, "com-trang.png", "Cơm trắng ngon."),
                };

                var library = libraryImages.ToHashSet(StringComparer.Ordinal);
                var createdAt = DateTime.UtcNow;
                for (var i = 0; i < seed.Length; i++)
                {
                    var s = seed[i];
                    var imageUrl = $"/{Patterns.LibraryFolder}/{s.Image}";
                    var product = new Product
                    {
                        Category = categories[s.Category],
                        Price = s.Price,
                        Rating = s.Rating,
                        IsFeatured = s.Featured,
                        Description = s.Description,
                        // Giữ thứ tự hiển thị như danh sách trên (món ở cuối danh sách = mới nhất)
                        CreatedAt = createdAt.AddSeconds(i),
                        // Ưu tiên tên file ghi rõ; nếu không có thì thử file trùng tên món (ví dụ "pepsi.png")
                        ImageUrl = library.Contains(imageUrl) ? imageUrl : imageBySlug.GetValueOrDefault(TextUtil.Slug(s.Name))
                    };
                    product.SetName(s.Name);
                    db.Products.Add(product);
                }
            }

            await db.SaveChangesAsync();
        }
    }
}