using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodieAPI.Data;
using FoodieAPI.Models;
using FoodieAPI.DTOs;
using FoodieAPI.Common;

namespace FoodieAPI.Endpoints
{
    public static class AdminEndpoints
    {
        public static void MapAdmin(this IEndpointRouteBuilder app)
        {
            var admin = app.MapGroup("/api/admin").RequireAuthorization("Admin");

            admin.MapGet("/stats", Stats);
            admin.MapGet("/orders", Orders);

            admin.MapGet("/products", ListProducts);
            admin.MapPost("/products", CreateProduct);
            admin.MapPut("/products/{id:int}", UpdateProduct);
            admin.MapDelete("/products/{id:int}", DeleteProduct);

            admin.MapPost("/uploads", Upload).DisableAntiforgery();
        }

        private static async Task<IResult> Stats(AppDbContext db)
        {
            var now = DateTime.UtcNow;
            var thisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var prevMonth = thisMonth.AddMonths(-1);
            var nextMonth = thisMonth.AddMonths(1);

            async Task<long> Revenue(DateTime from, DateTime to) =>
                await db.Orders
                    .Where(o => o.Status == OrderStatuses.Completed && o.CreatedAt >= from && o.CreatedAt < to)
                    .SumAsync(o => o.Total);

            async Task<int> ActiveOrders(DateTime from, DateTime to) =>
                await db.Orders.CountAsync(o =>
                    o.Status != OrderStatuses.Cancelled && o.CreatedAt >= from && o.CreatedAt < to);

            async Task<int> NewCustomers(DateTime from, DateTime to) =>
                await db.Users.CountAsync(u => u.Role == Roles.Customer && u.CreatedAt >= from && u.CreatedAt < to);

            var revenueNow = await Revenue(thisMonth, nextMonth);
            var revenuePrev = await Revenue(prevMonth, thisMonth);
            var ordersNow = await ActiveOrders(thisMonth, nextMonth);
            var ordersPrev = await ActiveOrders(prevMonth, thisMonth);
            var customersNow = await NewCustomers(thisMonth, nextMonth);
            var customersPrev = await NewCustomers(prevMonth, thisMonth);
            var totalCustomers = await db.Users.CountAsync(u => u.Role == Roles.Customer);

            return Results.Ok(new StatsDto(
                new Metric(revenueNow, Growth(revenueNow, revenuePrev)),
                new Metric(ordersNow, Growth(ordersNow, ordersPrev)),
                new Metric(totalCustomers, Growth(customersNow, customersPrev))));
        }

        private static double Growth(long current, long previous) =>
            previous == 0
                ? (current > 0 ? 100 : 0)
                : Math.Round((double)(current - previous) / previous * 100, 1);

        private static async Task<IResult> Orders(AppDbContext db, int? page, int? pageSize, string? status, string? keyword)
        {
            if (!string.IsNullOrWhiteSpace(status) && !OrderStatuses.IsValid(status))
                throw ApiException.Validation("status", "Trạng thái đơn hàng không hợp lệ.");

            var (p, size, skip) = Paging.Normalize(page, pageSize);
            var query = db.Orders.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            var kw = TextUtil.Keyword(keyword);
            if (kw is not null)
                query = query.Where(o => o.SearchText.Contains(kw));

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
                .Skip(skip).Take(size)
                .ToListAsync();

            return Results.Ok(new Paged<AdminOrderDto>(items.Select(o => o.ToAdminDto()).ToList(), total, p, size));
        }

        private static async Task<IResult> ListProducts(AppDbContext db, int? page, int? pageSize, string? category, string? keyword) =>
            Results.Ok(await ProductQueries.PagedAsync(db, category, keyword, page, pageSize));

        private static async Task<IResult> CreateProduct(ProductRequest req, AppDbContext db)
        {
            var input = ParseProduct(req);
            var product = new Product { CreatedAt = DateTime.UtcNow };
            Apply(product, input);

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return Results.Created($"/api/products/{product.Id}", product.ToDto());
        }

        private static async Task<IResult> UpdateProduct(int id, ProductRequest req, AppDbContext db)
        {
            var input = ParseProduct(req);
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new ApiException(404, "Không tìm thấy món ăn. Món có thể đã bị xóa.");

            Apply(product, input);
            await db.SaveChangesAsync();

            return Results.Ok(product.ToDto());
        }

        private static async Task<IResult> DeleteProduct(int id, AppDbContext db)
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new ApiException(404, "Không tìm thấy món ăn. Món có thể đã bị xóa.");

            db.Products.Remove(product);
            await db.SaveChangesAsync();

            return Results.NoContent();
        }

        private static ProductInput ParseProduct(ProductRequest r)
        {
            var name = (r.Name ?? "").Trim();
            var category = (r.Category ?? "").Trim();
            var description = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim();
            var imageUrl = string.IsNullOrWhiteSpace(r.ImageUrl) ? null : r.ImageUrl.Trim();

            new Validator()
                .Check(name.Length is >= 2 and <= 80, "name", "Tên món cần từ 2 đến 80 ký tự.")
                .Check(ProductCategories.IsValid(category), "category", "Danh mục không hợp lệ.")
                .Check(r.Price is >= 1000 and <= 10_000_000, "price", "Giá phải từ 1.000đ đến 10.000.000đ.")
                .Check((description?.Length ?? 0) <= 300, "description", "Mô tả tối đa 300 ký tự.")
                .Check(imageUrl is null || Patterns.UploadedImage.IsMatch(imageUrl),
                    "imageUrl", "Đường dẫn ảnh không hợp lệ. Hãy dùng ảnh đã tải lên từ hệ thống.")
                .ThrowIfInvalid();

            return new ProductInput(name, category, r.Price!.Value, description, imageUrl);
        }

        private static void Apply(Product product, ProductInput input)
        {
            product.SetName(input.Name);
            product.Category = input.Category;
            product.Price = input.Price;
            product.Description = input.Description;
            product.ImageUrl = input.ImageUrl;
        }

        private static async Task<IResult> Upload(IFormFile? file, IWebHostEnvironment env)
        {
            if (file is null)
                throw new ApiException(400, "Chưa chọn file ảnh (field \"file\").");
            if (file.Length <= 0)
                throw new ApiException(400, "File rỗng, vui lòng chọn ảnh khác.");
            if (file.Length > Limits.MaxImageBytes)
                throw new ApiException(400, "Ảnh vượt quá giới hạn 2MB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png"))
                throw new ApiException(400, "Chỉ chấp nhận ảnh định dạng .jpg, .jpeg, .png.");

            await using var input = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await input.CopyToAsync(buffer);

            if (buffer.Length > Limits.MaxImageBytes)
                throw new ApiException(400, "Ảnh vượt quá giới hạn 2MB.");

            var kind = ImageSniffer.Detect(buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, 8)));
            if (kind is null)
                throw new ApiException(400, "Nội dung file không phải ảnh JPG hoặc PNG hợp lệ.");

            var extMatchesContent = kind == "png" ? ext == ".png" : ext is ".jpg" or ".jpeg";
            if (!extMatchesContent)
                throw new ApiException(400, "Đuôi file không khớp với nội dung ảnh.");

            var folder = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}.{kind}";
            buffer.Position = 0;
            await using (var output = new FileStream(
                Path.Combine(folder, fileName), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await buffer.CopyToAsync(output);
            }

            return Results.Ok(new UploadResult($"/uploads/{fileName}"));
        }
    }
}