using System.ComponentModel.DataAnnotations;

namespace FoodieAPI.DTOs
{
    public record OrderItemRequest(
        [property: Range(1, int.MaxValue, ErrorMessage = "Mã món không hợp lệ.")]
        int ProductId,

        [property: Range(1, 99, ErrorMessage = "Mỗi món từ 1 đến 99 phần.")]
        int Quantity
    );

    public record OrderRequest(
        [property: Required(ErrorMessage = "Giỏ hàng trống.")]
        [property: Length(1, 50, ErrorMessage = "Giỏ hàng trống hoặc vượt quá 50 món.")]
        List<OrderItemRequest>? Items,

        [property: Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
        [property: StringLength(100, MinimumLength = 2, ErrorMessage = "Tên người nhận cần từ 2 đến 100 ký tự.")]
        string? ReceiverName,

        [property: Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [property: RegularExpression(@"^\s*(0|\+84)\d{9}\s*$", ErrorMessage = "Số điện thoại chưa đúng (ví dụ 0901234567).")]
        string? Phone,

        [property: Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        [property: StringLength(250, ErrorMessage = "Địa chỉ tối đa 250 ký tự.")]
        string? Address,

        [property: StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
        string? Note,

        [property: Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        [property: RegularExpression("^(cod|bank)$", ErrorMessage = "Phương thức thanh toán không hợp lệ.")]
        string? PaymentMethod
    );

    public record OrderCreated(string OrderCode, long Total, string Status, DateTime CreatedAt);

    public record AdminOrderDto(
        string Code,
        string Customer,
        DateTime CreatedAt,
        long Total,
        string Payment,
        string Status
    );

    public record OrderItemDto(int? ProductId, string ProductName, long UnitPrice, int Quantity, long LineTotal);

    public record OrderDetailDto(
        string Code,
        string CustomerName,
        string Phone,
        string Address,
        string? Note,
        string PaymentMethod,
        string Status,
        long Subtotal,
        long ShippingFee,
        long Total,
        DateTime CreatedAt,
        string AccountEmail,
        IReadOnlyList<string> NextStatuses,     // các trạng thái admin được phép chuyển tới
        IReadOnlyList<OrderItemDto> Items
    );

    /// <summary>Tóm tắt đơn cho trang "Đơn của tôi".</summary>
    public record MyOrderDto(string Code, DateTime CreatedAt, long Total, string Status, int ItemCount);

    /// <summary>Tiêu chí lọc danh sách đơn (admin).</summary>
    public record OrderQuery(string? Status = null, string? Keyword = null, int? Page = null, int? PageSize = null);

    public record OrderStatusRequest(
        [property: Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
        string? Status
    );

    public record Metric(long Value, double Growth);

    public record StatsDto(Metric Revenue, Metric Orders, Metric Customers);

    public record Paged<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
}
