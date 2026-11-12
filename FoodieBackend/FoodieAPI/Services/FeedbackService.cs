using Microsoft.EntityFrameworkCore;
using FoodieAPI.Common;
using FoodieAPI.Data;
using FoodieAPI.DTOs;
using FoodieAPI.Models;

namespace FoodieAPI.Services
{
    public interface IFeedbackService
    {
        /// <summary>Khách gửi góp ý. userId = null nếu chưa đăng nhập.</summary>
        Task<FeedbackDto> CreateAsync(FeedbackRequest req, int? userId);

        Task<Paged<FeedbackDto>> SearchAsync(FeedbackQuery query);
        Task<int> CountUnreadAsync();
        Task<FeedbackDto> SetReadAsync(int id, bool read);
        Task DeleteAsync(int id);
    }

    public sealed class FeedbackService(AppDbContext db, ILogger<FeedbackService> logger, ICurrentUser currentUser)
        : IFeedbackService
    {
        private const string NotFoundMessage = "Không tìm thấy góp ý.";

        public async Task<FeedbackDto> CreateAsync(FeedbackRequest req, int? userId)
        {
            var name = req.Name!.Trim();
            var message = req.Message!.Trim();
            new RuleChecker()
                .Check(name.Length >= 2, "name", "Họ tên cần từ 2 đến 100 ký tự.")
                .Check(message.Length >= 10, "message", "Nội dung cần từ 10 đến 1000 ký tự.")
                .ThrowIfInvalid();

            var feedback = new Feedback
            {
                UserId = userId,
                Name = name,
                Email = req.Email!.Trim().ToLowerInvariant(),
                Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim(),
                Topic = req.Topic!,
                Rating = req.Rating,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };

            db.Feedbacks.Add(feedback);
            await db.SaveChangesAsync();

            logger.LogInformation("Nhận góp ý #{Id} chủ đề {Topic} từ {Sender}",
                feedback.Id, feedback.Topic, userId is null ? "khách vãng lai" : $"user#{userId}");
            return ToDto(feedback);
        }

        public async Task<Paged<FeedbackDto>> SearchAsync(FeedbackQuery query)
        {
            var (page, size, skip) = Paging.Normalize(query.Page, query.PageSize);
            var q = db.Feedbacks.AsNoTracking();

            if (query.Unread == true) q = q.Where(f => !f.IsRead);

            var kw = query.Keyword?.Trim();
            if (!string.IsNullOrEmpty(kw))
                q = q.Where(f => f.Name.Contains(kw) || f.Email.Contains(kw) || f.Message.Contains(kw));

            var total = await q.CountAsync();
            var items = await q
                .OrderBy(f => f.IsRead).ThenByDescending(f => f.CreatedAt).ThenByDescending(f => f.Id)
                .Skip(skip).Take(size)
                .ToListAsync();

            return new Paged<FeedbackDto>(items.Select(ToDto).ToList(), total, page, size);
        }

        public Task<int> CountUnreadAsync() => db.Feedbacks.CountAsync(f => !f.IsRead);

        public async Task<FeedbackDto> SetReadAsync(int id, bool read)
        {
            var feedback = await db.Feedbacks.FirstOrDefaultAsync(f => f.Id == id)
                ?? throw ApiException.NotFound(NotFoundMessage);
            feedback.IsRead = read;
            await db.SaveChangesAsync();
            return ToDto(feedback);
        }

        public async Task DeleteAsync(int id)
        {
            var feedback = await db.Feedbacks.FirstOrDefaultAsync(f => f.Id == id)
                ?? throw ApiException.NotFound(NotFoundMessage);
            db.Feedbacks.Remove(feedback);
            await db.SaveChangesAsync();
            logger.LogInformation("{Actor} xóa góp ý #{Id}", currentUser.Describe(), id);
        }

        private static FeedbackDto ToDto(Feedback f) =>
            new(f.Id, f.Name, f.Email, f.Phone, f.Topic, f.Rating, f.Message, f.IsRead, f.UserId is not null, f.CreatedAt);
    }
}
