using FoodieAPI.Common;

namespace FoodieAPI.Services
{
    /// <summary>Lưu / xóa ảnh món ăn trong wwwroot/uploads, có kiểm tra an toàn.</summary>
    public interface IImageStorage
    {
        /// <summary>Kiểm tra (đuôi, kích thước, nội dung thật) rồi lưu ảnh; trả về URL dạng /uploads/{guid}.png.</summary>
        Task<string> SaveAsync(IFormFile? file, CancellationToken ct = default);

        /// <summary>Xóa ảnh cũ. Chỉ xóa file do hệ thống sinh ra, bỏ qua mọi đường dẫn lạ.</summary>
        void Delete(string? url);
    }

    public sealed class ImageStorage(IWebHostEnvironment env, ILogger<ImageStorage> logger, ICurrentUser currentUser)
        : IImageStorage
    {
        private string Folder => Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");

        public async Task<string> SaveAsync(IFormFile? file, CancellationToken ct = default)
        {
            if (file is null)
                throw new ApiException(400, "Chưa chọn file ảnh (field \"file\").");
            if (file.Length <= 0)
                throw new ApiException(400, "File rỗng, vui lòng chọn ảnh khác.");
            if (file.Length > Limits.MaxImageBytes)
                throw new ApiException(400, "Ảnh vượt quá giới hạn 2MB.");

            // 1. Kiểm tra đuôi file
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png"))
                throw new ApiException(400, "Chỉ chấp nhận ảnh định dạng .jpg, .jpeg, .png.");

            await using var input = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await input.CopyToAsync(buffer, ct);

            if (buffer.Length > Limits.MaxImageBytes)
                throw new ApiException(400, "Ảnh vượt quá giới hạn 2MB.");

            // 2. Kiểm tra nội dung thật (magic bytes), không tin Content-Type hay tên file do client gửi
            var kind = ImageSniffer.Detect(buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, 8)));
            if (kind is null)
                throw new ApiException(400, "Nội dung file không phải ảnh JPG hoặc PNG hợp lệ.");

            var extMatchesContent = kind == "png" ? ext == ".png" : ext is ".jpg" or ".jpeg";
            if (!extMatchesContent)
                throw new ApiException(400, "Đuôi file không khớp với nội dung ảnh.");

            // 3. Tên file do server tự sinh: chặn path traversal và ghi đè file khác
            Directory.CreateDirectory(Folder);
            var fileName = $"{Guid.NewGuid():N}.{kind}";
            buffer.Position = 0;
            await using (var output = new FileStream(
                Path.Combine(Folder, fileName), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await buffer.CopyToAsync(output, ct);
            }

            logger.LogInformation("{Actor} tải lên ảnh {FileName} ({Bytes} byte)", currentUser.Describe(), fileName, buffer.Length);
            return $"/uploads/{fileName}";
        }

        public void Delete(string? url)
        {
            if (string.IsNullOrEmpty(url) || !Patterns.UploadedImage.IsMatch(url)) return;

            try
            {
                var path = Path.Combine(Folder, Path.GetFileName(url));
                if (File.Exists(path))
                {
                    File.Delete(path);
                    logger.LogInformation("Đã xóa ảnh không còn dùng {Url}", url);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Không làm hỏng thao tác chính chỉ vì xóa ảnh cũ thất bại
                logger.LogWarning(ex, "Không xóa được ảnh {Url}", url);
            }
        }
    }
}
