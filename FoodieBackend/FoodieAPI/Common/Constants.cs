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

        private static readonly string[] All = [Pending, Preparing, Delivering, Completed, Cancelled];
        public static bool IsValid(string? value) => value is not null && All.Contains(value);
    }

    public static class PaymentMethods
    {
        public const string Cod = "cod";
        public const string Bank = "bank";
        public static bool IsValid(string? value) => value is Cod or Bank;
    }

    public static class ProductCategories
    {
        private static readonly string[] All = ["Đồ ăn nhanh", "Món chính", "Đồ uống", "Tráng miệng"];
        public static bool IsValid(string? value) => value is not null && All.Contains(value);
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
        public static readonly Regex Phone = new(@"^(0|\+84)\d{9}$", RegexOptions.Compiled);
        public static readonly Regex UploadedImage = new(@"^/uploads/[0-9a-f]{32}\.(jpg|png)$", RegexOptions.Compiled);
    }
}