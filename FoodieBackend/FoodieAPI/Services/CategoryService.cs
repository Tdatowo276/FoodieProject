using Microsoft.EntityFrameworkCore;
using FoodieAPI.Common;
using FoodieAPI.Data;
using FoodieAPI.DTOs;
using FoodieAPI.Models;

namespace FoodieAPI.Services
{
    public interface ICategoryService
    {
        /// <summary>Danh sách danh mục; khách hàng chỉ nhận danh mục đang hoạt động.</summary>
        Task<List<CategoryDto>> GetAllAsync(bool includeInactive);
        Task<CategoryDto> GetAsync(int id);
        Task<CategoryDto> CreateAsync(CategoryRequest req);
        Task<CategoryDto> UpdateAsync(int id, CategoryRequest req);
        Task DeleteAsync(int id);
    }

    public sealed class CategoryService(AppDbContext db, ILogger<CategoryService> logger, ICurrentUser currentUser)
        : ICategoryService
    {
        public async Task<List<CategoryDto>> GetAllAsync(bool includeInactive) =>
            await db.Categories.AsNoTracking()
                .Where(c => includeInactive || c.IsActive)
                .OrderBy(c => c.Id)
                .Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.IsActive, c.Products.Count))
                .ToListAsync();

        public async Task<CategoryDto> GetAsync(int id) =>
            await db.Categories.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.IsActive, c.Products.Count))
                .FirstOrDefaultAsync()
            ?? throw ApiException.NotFound("Không tìm thấy danh mục.");

        public async Task<CategoryDto> CreateAsync(CategoryRequest req)
        {
            var category = new Category();
            await ApplyAsync(category, req);

            db.Categories.Add(category);
            await db.SaveChangesAsync();

            logger.LogInformation("{Actor} tạo danh mục #{Id} \"{Name}\"", currentUser.Describe(), category.Id, category.Name);
            return await GetAsync(category.Id);
        }

        public async Task<CategoryDto> UpdateAsync(int id, CategoryRequest req)
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id)
                ?? throw ApiException.NotFound("Không tìm thấy danh mục.");

            await ApplyAsync(category, req);
            await db.SaveChangesAsync();

            logger.LogInformation("{Actor} cập nhật danh mục #{Id} \"{Name}\" (hiển thị: {IsActive})",
                currentUser.Describe(), category.Id, category.Name, category.IsActive);
            return await GetAsync(category.Id);
        }

        public async Task DeleteAsync(int id)
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id)
                ?? throw ApiException.NotFound("Không tìm thấy danh mục.");

            // Ràng buộc FK là Restrict; kiểm tra trước để trả thông báo dễ hiểu thay vì lỗi SQL
            var productCount = await db.Products.CountAsync(p => p.CategoryId == id);
            if (productCount > 0)
                throw ApiException.Conflict(
                    $"Danh mục đang có {productCount} món. Hãy chuyển hoặc xóa các món đó trước, hoặc ẩn danh mục.");

            db.Categories.Remove(category);
            await db.SaveChangesAsync();

            logger.LogInformation("{Actor} xóa danh mục #{Id} \"{Name}\"", currentUser.Describe(), id, category.Name);
        }

        private async Task ApplyAsync(Category category, CategoryRequest req)
        {
            var name = req.Name!.Trim();
            new RuleChecker()
                .Check(name.Length >= 2, "name", "Tên danh mục cần từ 2 đến 50 ký tự.")
                .ThrowIfInvalid();

            // Collation mặc định của SQL Server không phân biệt hoa thường
            if (await db.Categories.AnyAsync(c => c.Name == name && c.Id != category.Id))
                throw ApiException.Validation("name", "Tên danh mục đã tồn tại.");

            category.Name = name;
            category.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
            category.IsActive = req.IsActive;
        }
    }
}
