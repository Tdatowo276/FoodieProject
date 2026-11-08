namespace FoodieAPI.DTOs
{
    public record JwtSettings(string Issuer, string Audience, string Key, int ExpiresMinutes);

    public record RegisterRequest(string? Name, string? Email, string? Password);

    public record LoginRequest(string? Email, string? Password);

    public record AuthUser(int Id, string Name, string Email, string Role);

    public record AuthResult(string Token, DateTime ExpiresAt, AuthUser User);
}