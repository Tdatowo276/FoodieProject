using System.Globalization;
using FoodieAPI.Common;

namespace FoodieAPI.Areas.Admin
{
    /// <summary>Định dạng hiển thị dùng chung cho các View trong khu Admin.</summary>
    public static class AdminFormat
    {
        private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

        public static string Money(long value) => value.ToString("N0", Vi) + "đ";

        /// <summary>CSDL lưu giờ UTC; Việt Nam là UTC+7 quanh năm (không có giờ mùa hè).</summary>
        public static string VnTime(DateTime utc) =>
            DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(7).ToString("dd/MM/yyyy HH:mm", Vi);

        /// <summary>Màu badge Bootstrap theo trạng thái đơn.</summary>
        public static string StatusColor(string status) => status switch
        {
            OrderStatuses.Pending => "warning",
            OrderStatuses.Preparing => "info",
            OrderStatuses.Delivering => "primary",
            OrderStatuses.Completed => "success",
            OrderStatuses.Cancelled => "secondary",
            _ => "light"
        };
    }
}
