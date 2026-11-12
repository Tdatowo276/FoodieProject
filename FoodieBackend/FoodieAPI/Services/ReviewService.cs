using Microsoft.EntityFrameworkCore;
using FoodieAPI.Common;
using FoodieAPI.Data;
using FoodieAPI.DTOs;
using FoodieAPI.Models;

namespace FoodieAPI.Services
{
    public interface IReviewService
    {
        /// <summary>Đánh giá công khai của một món (món phải đang bán) + tóm tắt điểm.</summary>
        Task<ProductReviewsDto> GetForProductAsync(int productId, int? page, int? pageSize);

        /// <summary>Người dùng đã được phép đánh giá chưa, và đánh giá hiện tại của họ (nếu có).</summary>
        Task<MyReviewDto> GetMineAsync(int productId, int userId);

        /// <summary>Thêm mới hoặc sửa đánh giá của chính mình (mỗi người 1 đánh giá / món).</summary>
        Task<ReviewDto> UpsertAsync(int productId, int userId, ReviewRequest req);

        Task DeleteMineAsync(int productId, int userId);

        // Quản trị
        Task<Paged<ReviewDto>> SearchAsync(ReviewQuery query);
        Task DeleteAsync(int reviewId);
    }

    public sealed class ReviewService(AppDbContext db, ILogger<ReviewService> logger, ICurrentUser currentUser)
        : IReviewService
    {
        private const string NotPurchased = "Bạn chỉ đánh giá được món đã mua và đã nhận hàng (đơn hoàn thành).";

        public async Task<ProductReviewsDto> GetForProductAsync(int productId, int? page, int? pageSize)
        {
            await EnsureVisibleProductAsync(productId);

            var (p, size, skip) = Paging.Normalize(page, pageSize);
            var q = db.Reviews.AsNoTracking().Where(r => r.ProductId == productId);

            // Số lượt theo từng mức sao (1..5)
            var byStar = await q.GroupBy(r => r.Rating).Select(g => new { Star = g.Key, Count = g.Count() }).ToListAsync();
            var stars = Enumerable.Range(1, 5).Select(s => byStar.FirstOrDefault(x => x.Star == s)?.Count ?? 0).ToArray();
            var count = stars.Sum();
            var average = count == 0 ? 0 : Math.Round(byStar.Sum(x => x.Star * x.Count) / (double)count, 1);

            var items = await q
                .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
                .Skip(skip).Take(size)
                .Select(ToDtoExpr)
                .ToListAsync();

            return new ProductReviewsDto(new RatingSummary(average, count, stars), new Paged<ReviewDto>(items, count, p, size));
        }

        public async Task<MyReviewDto> GetMineAsync(int productId, int userId)
        {
            await EnsureVisibleProductAsync(productId);

            var mine = await db.Reviews.AsNoTracking()
                .Where(r => r.ProductId == productId && r.UserId == userId)
                .Select(ToDtoExpr)
                .FirstOrDefaultAsync();

            var purchased = await HasCompletedPurchaseAsync(productId, userId);
            return new MyReviewDto(purchased, purchased ? null : NotPurchased, mine);
        }

        public async Task<ReviewDto> UpsertAsync(int productId, int userId, ReviewRequest req)
        {
            await EnsureVisibleProductAsync(productId);

            // Quy tắc nghiệp vụ kiểm tra phía server: không tin giao diện ẩn/hiện nút "Đánh giá"
            if (!await HasCompletedPurchaseAsync(productId, userId))
                throw new ApiException(403, NotPurchased);

            var comment = string.IsNullOrWhiteSpace(req.Comment) ? null : req.Comment.Trim();
            var review = await db.Reviews.FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);
            var isNew = review is null;

            if (review is null)
            {
                review = new Review { ProductId = productId, UserId = userId, CreatedAt = DateTime.UtcNow };
                db.Reviews.Add(review);
            }
            else
            {
                review.UpdatedAt = DateTime.UtcNow;
            }
            review.Rating = req.Rating!.Value;
            review.Comment = comment;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Hai request cùng lúc của cùng người: unique index (ProductId, UserId) chặn bản thứ hai
                throw ApiException.Conflict("Đánh giá vừa được lưu ở nơi khác. Vui lòng tải lại.");
            }

            await RecalculateProductRatingAsync(productId);
            logger.LogInformation("Người dùng {UserId} {Action} đánh giá {Rating} sao cho món #{ProductId}",
                userId, isNew ? "thêm" : "sửa", review.Rating, productId);

            return await db.Reviews.AsNoTracking().Where(r => r.Id == review.Id).Select(ToDtoExpr).SingleAsync();
        }

        public async Task DeleteMineAsync(int productId, int userId)
        {
            var review = await db.Reviews.FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId)
                ?? throw ApiException.NotFound("Bạn chưa đánh giá món này.");

            db.Reviews.Remove(review);
            await db.SaveChangesAsync();
            await RecalculateProductRatingAsync(productId);
        }

        public async Task<Paged<ReviewDto>> SearchAsync(ReviewQuery query)
        {
            var (page, size, skip) = Paging.Normalize(query.Page, query.PageSize);
            var q = db.Reviews.AsNoTracking();

            if (query.Rating is >= 1 and <= 5) q = q.Where(r => r.Rating == query.Rating);

            var kw = query.Keyword?.Trim();
            if (!string.IsNullOrEmpty(kw))
                q = q.Where(r => r.Product.Name.Contains(kw) || r.User.Username.Contains(kw) || (r.Comment != null && r.Comment.Contains(kw)));

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
                .Skip(skip).Take(size)
                .Select(ToDtoExpr)
                .ToListAsync();

            return new Paged<ReviewDto>(items, total, page, size);
        }

        public async Task DeleteAsync(int reviewId)
        {
            var review = await db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId)
                ?? throw ApiException.NotFound("Không tìm thấy đánh giá.");

            db.Reviews.Remove(review);
            await db.SaveChangesAsync();
            await RecalculateProductRatingAsync(review.ProductId);

            logger.LogInformation("{Actor} xóa đánh giá #{Id} của món #{ProductId}", currentUser.Describe(), reviewId, review.ProductId);
        }

        // ---------------------------------------------------------------------

        private static readonly System.Linq.Expressions.Expression<Func<Review, ReviewDto>> ToDtoExpr = r =>
            new ReviewDto(r.Id, r.ProductId, r.Product.Name, r.User.Username, r.User.AvatarUrl,
                r.Rating, r.Comment, r.CreatedAt, r.UpdatedAt);

        /// <summary>Đã từng mua món trong một đơn ĐÃ HOÀN THÀNH (đơn hủy / chưa giao không tính).</summary>
        private Task<bool> HasCompletedPurchaseAsync(int productId, int userId) =>
            db.OrderItems.AnyAsync(i =>
                i.ProductId == productId && i.Order.UserId == userId && i.Order.Status == OrderStatuses.Completed);

        // Món đang bán (danh mục đang hiển thị); món ẩn / đã xóa => 404
        private async Task EnsureVisibleProductAsync(int productId)
        {
            if (!await db.Products.AnyAsync(p => p.Id == productId && p.Category.IsActive))
                throw ApiException.NotFound("Không tìm thấy món ăn. Món có thể đã bị xóa.");
        }

        // Cập nhật điểm trung bình + số lượt lưu sẵn trên bảng Products
        private async Task RecalculateProductRatingAsync(int productId)
        {
            var stats = await db.Reviews.Where(r => r.ProductId == productId)
                .GroupBy(r => r.ProductId)
                .Select(g => new { Average = g.Average(r => (double)r.Rating), Count = g.Count() })
                .FirstOrDefaultAsync();

            var average = stats is null ? 0 : Math.Round(stats.Average, 1);
            var count = stats?.Count ?? 0;

            await db.Products.Where(p => p.Id == productId).ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Rating, average)
                .SetProperty(p => p.ReviewCount, count));
        }
    }
}
