namespace FoodieAPI.DTOs
{
    public record OrderItemRequest(int ProductId, int Quantity);

    public record OrderRequest(
        List<OrderItemRequest>? Items, 
        string? ReceiverName, 
        string? Phone,
        string? Address, 
        string? Note, 
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

    public record Metric(long Value, double Growth);

    public record StatsDto(Metric Revenue, Metric Orders, Metric Customers);

    public record Paged<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
}