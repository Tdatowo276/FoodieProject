using System.Text.RegularExpressions;

namespace FoodieAPI.Common
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Customer = "Customer";
    }

    public static class OrderStatuses
    {
        public const string Pending = "pending";
        public const string Preparing = "preparing";
        public const string Delivering = "delivering";
        public const string Completed = "completed";
        public const string Cancelled = "cancelled";

        public static readonly string[] All = [Pending, Preparing, Delivering, Completed, Cancelled];
        public static bool IsValid(string? value) => value is not null && All.Contains(value);

        /// <summary>
        /// Luồng xử lý đơn: pending -> preparing -> delivering -> completed.
        /// Chỉ hủy được khi đơn chưa giao. completed / cancelled là trạng thái cuối.
        /// </summary>
        public static bool CanChange(string from, string to) => (from, to) switch
        {
            (Pending, Preparing or Cancelled) => true,
            (Preparing, Delivering or Cancelled) => true,
            (Delivering, Completed) => true,
            _ => false
        };

        public static IReadOnlyList<string> NextOf(string from) => All.Where(to => CanChange(from, to)).ToList();

        public static string Label(string status) => status switch
        {
            Pending => "Chờ xác nhận",
            Preparing => "Đang chuẩn bị",
            Delivering => "Đang giao",
            Completed => "Hoàn thành",
            Cancelled => "Đã hủy",
            _ => status
        };
    }

    public static class PaymentMethods
    {
        public const string Cod = "cod";
        public const string Bank = "bank";

        public static string Label(string method) => method switch
        {
            Cod => "Tiền mặt khi nhận hàng",
            Bank => "Chuyển khoản",
            _ => method
        };
    }

    public static class RateLimitPolicies
    {
        public const string Auth = "auth";
        public const string Feedback = "feedback";
    }

    public static class Limits
    {
        public const int MaxImageBytes = 2 * 1024 * 1024;   // 2MB
        public const int MaxRequestBytes = 3 * 1024 * 1024; // 3MB
        public const long FreeShippingFrom = 200_000;
        public const long ShippingFee = 15_000;
    }

    public static class Patterns
    {
        public static readonly Regex Email = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        // Đường dẫn ảnh do hệ thống tự sinh khi upload (GUID 32 ký tự hex)
        public const string UploadedImagePattern = @"^/uploads/[0-9a-f]{32}\.(jpg|png)$";
        public static readonly Regex UploadedImage = new(UploadedImagePattern, RegexOptions.Compiled);

        // Ảnh trong thư viện ảnh món của nhóm (wwwroot/images/products, có trên Git).
        // Tên file chỉ gồm chữ thường không dấu, số, gạch ngang => không thể chèn "../" hay ký tự lạ.
        public const string LibraryFolder = "images/products";
        public const string LibraryImagePattern = @"^/images/products/[a-z0-9]+(-[a-z0-9]+)*\.(jpg|jpeg|png|webp)$";
        public static readonly Regex LibraryImage = new(LibraryImagePattern, RegexOptions.Compiled);

        // Ảnh món hợp lệ: ảnh upload HOẶC ảnh trong thư viện
        public const string ProductImagePattern =
            @"^(/uploads/[0-9a-f]{32}\.(jpg|png)|/images/products/[a-z0-9]+(-[a-z0-9]+)*\.(jpg|jpeg|png|webp))$";
    }
}