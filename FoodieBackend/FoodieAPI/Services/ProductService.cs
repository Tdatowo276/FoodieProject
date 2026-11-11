using Microsoft.EntityFrameworkCore;
using FoodieAPI.Common;
using FoodieAPI.Data;
using FoodieAPI.DTOs;
using FoodieAPI.Models;

namespace FoodieAPI.Services
{
    public interface IProductService
    {
        /// <summary>
        /// Tìm kiếm / lọc / phân trang món. includeHidden = true cho admin
        /// (thấy cả món thuộc danh mục đang ẩn).
        /// </summary>
        Task<Paged<ProductDto>> SearchAsync(ProductQuery query, bool includeHidden);

        /// <summary>Danh sách không phân trang (tối đa 200 món), dành cho khách.</summary>
        Task<List<ProductDto>> ListAsync(ProductQuery query);

        Task<List<ProductDto>> GetFeaturedAsync(int take = 8);
        Task<ProductDto> GetAsync(int id, bool includeHidden);
        Task<ProductDto> CreateAsync(ProductRequest req);
        Task<ProductDto> UpdateAsync(int id, ProductRequest req);
        Task DeleteAsync(int id);
    }

    public sealed class ProductService(
        AppDbContext db, IImageStorage images, ILogger<ProductService> logger, ICurrentUser currentUser)
        : IProductService
    {
        private const int MaxUnpagedItems = 200;
        private const string NotFoundMessage = "Không tìm thấy món ăn. Món có thể đã bị xóa.";

        public async Task<Paged<ProductDto>> SearchAsync(ProductQuery query, bool includeHidden)
        {
            var (page, size, skip) = Paging.Normalize(query.Page, query.PageSize);
            var q = Filter(Base(includeHidden), query);

            var total = await q.CountAsync();
            var items = await Newest(q).Skip(skip).Take(size).ToListAsync();

            return new Paged<ProductDto>(items.Select(p => p.ToDto()).ToList(), total, page, size);
        }

        public async Task<List<ProductDto>> ListAsync(ProductQuery query)
        {
            var items = await Newest(Filter(Base(includeHidden: false), query)).Take(MaxUnpagedItems).ToListAsync();
            return items.Select(p => p.ToDto()).ToList();
        }

        public async Task<List<ProductDto>> GetFeaturedAsync(int take = 8)
        {
            var items = await Base(includeHidden: false)
                .Where(p => p.IsFeatured)
                .OrderByDescending(p => p.Rating).ThenBy(p => p.Id)
                .Take(take)
                .ToListAsync();
            return items.Select(p => p.ToDto()).ToList();
        }

        public async Task<ProductDto> GetAsync(int id, bool includeHidden)
        {
            var product = await Base(includeHidden).FirstOrDefaultAsync(p => p.Id == id)
                ?? throw ApiException.NotFound(NotFoundMessage);
            return product.ToDto();
        }

        public async Task<ProductDto> CreateAsync(ProductRequest req)
        {
            var product = new Product { CreatedAt = DateTime.UtcNow };
            await ApplyAsync(product, req);

            db.Products.Add(product);
            await db.SaveChangesAsync();

            logger.LogInformation("{Actor} thêm món #{Id} \"{Name}\" giá {Price}đ",
                currentUser.Describe(), product.Id, product.Name, product.Price);
            return product.ToDto();
        }

        public async Task<ProductDto> UpdateAsync(int id, ProductRequest req)
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id)
                ?? throw ApiException.NotFound(NotFoundMessage);

            var oldImage = product.ImageUrl;
            await ApplyAsync(product, req);
            await db.SaveChangesAsync();

            // Chỉ xóa ảnh cũ sau khi lưu CSDL thành công
            if (oldImage != product.ImageUrl) await DeleteImageIfUnusedAsync(oldImage);

            logger.LogInformation("{Actor} cập nhật món #{Id} \"{Name}\"", currentUser.Describe(), product.Id, product.Name);
            return product.ToDto();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id)
                ?? throw ApiException.NotFound(NotFoundMessage);

            // Đơn cũ vẫn giữ tên + giá món (OrderItem.ProductId chuyển thành NULL)
            db.Products.Remove(product);
            await db.SaveChangesAsync();
            await DeleteImageIfUnusedAsync(product.ImageUrl);

            logger.LogInformation("{Actor} xóa món #{Id} \"{Name}\"", currentUser.Describe(), id, product.Name);
        }

        // ---------------------------------------------------------------------

        private async Task DeleteImageIfUnusedAsync(string? url)
        {
            if (url is null || await db.Products.AnyAsync(p => p.ImageUrl == url)) return;
            images.Delete(url);
        }

        // Khách chỉ thấy món thuộc danh mục đang hoạt động
        private IQueryable<Product> Base(bool includeHidden) =>
            db.Products.AsNoTracking().Include(p => p.Category)
                .Where(p => includeHidden || p.Category.IsActive);

        private static IQueryable<Product> Newest(IQueryable<Product> q) =>
            q.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id);

        private static IQueryable<Product> Filter(IQueryable<Product> q, ProductQuery query)
        {
            if (query.CategoryId is > 0)
                q = q.Where(p => p.CategoryId == query.CategoryId);

            var category = query.Category?.Trim();
            if (!string.IsNullOrEmpty(category))
                q = q.Where(p => p.Category.Name == category);

            // So khớp không dấu: "pho" hoặc "phở" đều tìm được "Phở bò"
            var kw = TextUtil.Keyword(query.Keyword);
            if (kw is not null)
                q = q.Where(p => p.SearchText.Contains(kw));

            return q;
        }

        // Định dạng đã được Data Annotations của ProductRequest kiểm tra.
        // Ở đây chỉ kiểm tra các quy tắc cần tới CSDL hoặc cần dữ liệu đã chuẩn hóa.
        private async Task ApplyAsync(Product product, ProductRequest r)
        {
            var name = r.Name!.Trim();
            new RuleChecker()
                .Check(name.Length >= 2, "name", "Tên món cần từ 2 đến 80 ký tự.")
                .ThrowIfInvalid();

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
    }
}
