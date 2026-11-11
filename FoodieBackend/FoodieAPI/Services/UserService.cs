using Microsoft.EntityFrameworkCore;
using FoodieAPI.Common;
using FoodieAPI.Data;
using FoodieAPI.DTOs;

namespace FoodieAPI.Services
{
    /// <summary>Quản lý tài khoản (admin).</summary>
    public interface IUserService
    {
        Task<Paged<UserDto>> SearchAsync(UserQuery query);

        /// <summary>Khóa / mở khóa tài khoản khách hàng. Không áp dụng cho tài khoản admin.</summary>
        Task<UserDto> SetLockedAsync(int id, bool locked);
    }

    public sealed class UserService(AppDbContext db, ILogger<UserService> logger, ICurrentUser currentUser)
        : IUserService
    {
        public async Task<Paged<UserDto>> SearchAsync(UserQuery query)
        {
            if (!string.IsNullOrWhiteSpace(query.Role) && query.Role is not (Roles.Admin or Roles.Customer))
                throw ApiException.Validation("role", "Vai trò không hợp lệ.");

            var (page, size, skip) = Paging.Normalize(query.Page, query.PageSize);
            var q = db.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Role))
                q = q.Where(u => u.Role == query.Role);

            var kw = query.Keyword?.Trim();
            if (!string.IsNullOrEmpty(kw))
                q = q.Where(u => u.Email.Contains(kw) || u.Username.Contains(kw));

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.Id)
                .Skip(skip).Take(size)
                .Select(u => new UserDto(u.Id, u.Username, u.Email, u.Role, u.IsLocked, u.CreatedAt, u.Orders.Count))
                .ToListAsync();

            return new Paged<UserDto>(items, total, page, size);
        }

        public async Task<UserDto> SetLockedAsync(int id, bool locked)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id)
                ?? throw ApiException.NotFound("Không tìm thấy tài khoản.");

            // Không cho khóa admin (kể cả tự khóa mình) để tránh mất quyền quản trị hệ thống
            if (user.Role == Roles.Admin)
                throw new ApiException(400, "Không thể khóa tài khoản quản trị viên.");

            if (user.IsLocked != locked)
            {
                user.IsLocked = locked;
                await db.SaveChangesAsync();
                logger.LogInformation("{Actor} {Action} tài khoản {UserId}",
                    currentUser.Describe(), locked ? "khóa" : "mở khóa", id);
            }

            var orderCount = await db.Orders.CountAsync(o => o.UserId == id);
            return new UserDto(user.Id, user.Username, user.Email, user.Role, user.IsLocked, user.CreatedAt, orderCount);
        }
    }
}
