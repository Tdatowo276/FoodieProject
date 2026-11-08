using System.Globalization;
using System.Text;
using FoodieAPI.Models;
using FoodieAPI.DTOs;

namespace FoodieAPI.Common
{
    public static class TextUtil
    {
        public static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            var decomposed = text.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Replace('đ', 'd').Replace('Đ', 'D').ToLowerInvariant();
        }

        public static string? Keyword(string? keyword)
        {
            var normalized = Normalize(keyword);
            if (normalized.Length == 0) return null;
            return normalized.Length > 100 ? normalized[..100] : normalized;
        }
    }

    public static class Paging
    {
        public static (int Page, int PageSize, int Skip) Normalize(int? page, int? pageSize)
        {
            var p = Math.Clamp(page ?? 1, 1, 100_000);
            var s = Math.Clamp(pageSize ?? 10, 1, 50);
            return (p, s, (p - 1) * s);
        }
    }

    public static class PasswordTools
    {
        public static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("foodie-dummy-password");
    }

    public static class ImageSniffer
    {
        private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        public static string? Detect(ReadOnlySpan<byte> header)
        {
            if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return "jpg";

            if (header.Length >= PngSignature.Length && header[..PngSignature.Length].SequenceEqual(PngSignature))
                return "png";

            return null;
        }
    }

    public static class Mappings
    {
        public static ProductDto ToDto(this Product p) =>
            new(p.Id, p.Name, p.Category, p.Price, p.Rating, p.Description, p.ImageUrl, p.IsFeatured, p.CreatedAt);

        public static AdminOrderDto ToAdminDto(this Order o) =>
            new(o.Code, o.CustomerName, o.CreatedAt, o.Total, o.PaymentMethod, o.Status);
    }

}