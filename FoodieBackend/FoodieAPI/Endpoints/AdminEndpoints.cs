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
            admin.MapPost("/products", CreateProduct).Validate<ProductRequest>();
            admin.MapPut("/products/{id:int}", UpdateProduct).Validate<ProductRequest>();
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

        // Admin thấy mọi món, kể cả món thuộc danh mục đang ẩn
        private static async Task<IResult> ListProducts(
            AppDbContext db, int? page, int? pageSize, int? categoryId, string? category, string? keyword)
        {
            var query = ProductQueries.Filter(
                db.Products.AsNoTracking().Include(p => p.Category), categoryId, category, keyword);
            return Results.Ok(await ProductQueries.PagedAsync(query, page, pageSize));
        }

        private static async Task<IResult> CreateProduct(ProductRequest req, AppDbContext db)
        {
            var product = new Product { CreatedAt = DateTime.UtcNow };
            await ApplyAsync(product, req, db);

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return Results.Created($"/api/products/{product.Id}", product.ToDto());
        }

        private static async Task<IResult> UpdateProduct(int id, ProductRequest req, AppDbContext db)
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new ApiException(404, "Không tìm thấy món ăn. Món có thể đã bị xóa.");

            await ApplyAsync(product, req, db);
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

        // Định dạng đã được Data Annotations của ProductRequest kiểm tra (ValidationFilter).
        // Ở đây chỉ kiểm tra các quy tắc cần tới CSDL hoặc cần dữ liệu đã chuẩn hóa.
        private static async Task ApplyAsync(Product product, ProductRequest r, AppDbContext db)
        {
            var name = r.Name!.Trim();
            if (name.Length < 2)
                throw ApiException.Validation("name", "Tên món cần từ 2 đến 80 ký tự.");

            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == r.CategoryId)
                ?? throw ApiException.Validation("categoryId", "Danh mục không tồn tại.");

            // Chỉ chặn khi chọn MỚI một danh mục đang ẩn; món cũ vẫn sửa được dù danh mục đã ẩn
            if (!category.IsActive && product.CategoryId != category.Id)
                throw ApiException.Validation("categoryId", "Danh mục này đang bị ẩn, hãy chọn danh mục khác.");

            product.SetName(name);
            product.Category = category;
            product.Price = r.Price!.Value;
            product.Description = string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim();
            product.ImageUrl = string.IsNullOrWhiteSpace(r.ImageUrl) ? null : r.ImageUrl.Trim();
            product.IsFeatured = r.IsFeatured;
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