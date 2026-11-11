using Microsoft.EntityFrameworkCore;
using FoodieAPI.Common;
using FoodieAPI.Data;
using FoodieAPI.DTOs;
using FoodieAPI.Models;

namespace FoodieAPI.Services
{
    /// <summary>
    /// Hồ sơ cá nhân: họ tên, ảnh đại diện, mật khẩu, địa chỉ giao hàng mặc định.
    /// Mọi hàm nhận userId lấy từ token (ICurrentUser) => người dùng chỉ sửa được hồ sơ của chính mình.
    /// </summary>
    public interface IProfileService
    {
        Task<ProfileDto> GetAsync(int userId);
        Task<ProfileDto> UpdateAsync(int userId, UpdateProfileRequest req);
        Task ChangePasswordAsync(int userId, ChangePasswordRequest req);
        Task<ProfileDto> SetAvatarAsync(int userId, IFormFile? file, CancellationToken ct = default);
        Task<ProfileDto> RemoveAvatarAsync(int userId);
        Task<ProfileDto> SetDefaultAddressAsync(int userId, AddressRequest req);
        Task<ProfileDto> ClearDefaultAddressAsync(int userId);
    }

    public sealed class ProfileService(AppDbContext db, IImageStorage images, ILogger<ProfileService> logger)
        : IProfileService
    {
        public async Task<ProfileDto> GetAsync(int userId) => ToDto(await FindAsync(userId, tracking: false));

        public async Task<ProfileDto> UpdateAsync(int userId, UpdateProfileRequest req)
        {
            var name = req.Name!.Trim();
            new RuleChecker().Check(name.Length >= 2, "name", "Họ tên cần từ 2 đến 100 ký tự.").ThrowIfInvalid();

            var user = await FindAsync(userId);
            user.Username = name;
            await db.SaveChangesAsync();

            logger.LogInformation("Người dùng {UserId} đổi họ tên", userId);
            return ToDto(user);
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordRequest req)
        {
            var user = await FindAsync(userId);

            // Bắt buộc đúng mật khẩu hiện tại: người khác cầm được token (máy dùng chung...) cũng không đổi được mật khẩu
            if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            {
                logger.LogWarning("Người dùng {UserId} đổi mật khẩu thất bại: sai mật khẩu hiện tại", userId);
                throw ApiException.Validation("currentPassword", "Mật khẩu hiện tại chưa đúng.");
            }
            if (req.NewPassword == req.CurrentPassword)
                throw ApiException.Validation("newPassword", "Mật khẩu mới phải khác mật khẩu hiện tại.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
            await db.SaveChangesAsync();
            logger.LogInformation("Người dùng {UserId} đổi mật khẩu thành công", userId);
        }

        public async Task<ProfileDto> SetAvatarAsync(int userId, IFormFile? file, CancellationToken ct = default)
        {
            var user = await FindAsync(userId);

            // Cùng cơ chế kiểm tra an toàn như ảnh món: đuôi file, tối đa 2MB, nội dung thật (magic bytes), tên GUID
            var url = await images.SaveAsync(file, ct);
            var old = user.AvatarUrl;
            user.AvatarUrl = url;

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch
            {
                images.Delete(url);   // lưu CSDL thất bại => không để lại ảnh mồ côi
                throw;
            }

            images.Delete(old);       // ảnh đại diện cũ không ai dùng chung => xóa luôn
            logger.LogInformation("Người dùng {UserId} đổi ảnh đại diện", userId);
            return ToDto(user);
        }

        public async Task<ProfileDto> RemoveAvatarAsync(int userId)
        {
            var user = await FindAsync(userId);
            var old = user.AvatarUrl;
            user.AvatarUrl = null;
            await db.SaveChangesAsync();
            images.Delete(old);
            return ToDto(user);
        }

        public async Task<ProfileDto> SetDefaultAddressAsync(int userId, AddressRequest req)
        {
            var recipient = req.RecipientName!.Trim();
            var address = req.Address!.Trim();
            new RuleChecker()
                .Check(recipient.Length >= 2, "recipientName", "Tên người nhận cần từ 2 đến 100 ký tự.")
                .Check(address.Length >= 1, "address", "Vui lòng nhập địa chỉ.")
                .ThrowIfInvalid();

            var user = await FindAsync(userId);
            user.DefaultRecipientName = recipient;
            user.DefaultPhone = req.Phone!.Trim();
            user.DefaultAddress = address;
            await db.SaveChangesAsync();

            logger.LogInformation("Người dùng {UserId} cập nhật địa chỉ mặc định", userId);
            return ToDto(user);
        }

        public async Task<ProfileDto> ClearDefaultAddressAsync(int userId)
        {
            var user = await FindAsync(userId);
            user.DefaultRecipientName = user.DefaultPhone = user.DefaultAddress = null;
            await db.SaveChangesAsync();
            return ToDto(user);
        }

        // ---------------------------------------------------------------------

        private async Task<User> FindAsync(int userId, bool tracking = true)
        {
            var query = tracking ? db.Users : db.Users.AsNoTracking();
            return await query.FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new ApiException(401, "Tài khoản không còn tồn tại. Vui lòng đăng nhập lại.");
        }

        private static ProfileDto ToDto(User u) => new(
            u.Id, u.Username, u.Email, u.Role, u.AvatarUrl,
            u.DefaultAddress is null ? null : new AddressDto(u.DefaultRecipientName ?? "", u.DefaultPhone ?? "", u.DefaultAddress),
            u.CreatedAt);
    }
}
