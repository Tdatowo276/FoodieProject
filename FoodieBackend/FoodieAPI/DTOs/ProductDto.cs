namespace FoodieAPI.DTOs
{
    public record ProductDto(
        int Id, 
        string Name, 
        string Category, 
        long Price, 
        double Rating,
        string? Description, 
        string? ImageUrl, 
        bool IsFeatured, 
        DateTime CreatedAt
    );

    public record ProductRequest(
        string? Name, 
        string? Category, 
        long? Price, 
        string? Description, 
        string? ImageUrl
    );

    public record ProductInput(
        string Name, 
        string Category, 
        long Price, 
        string? Description, 
        string? ImageUrl
    );

    public record UploadResult(string Url);
}