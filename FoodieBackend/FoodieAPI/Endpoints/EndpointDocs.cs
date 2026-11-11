namespace FoodieAPI.Endpoints
{
    /// <summary>Khai báo các mã lỗi chuẩn cho tài liệu Swagger/OpenAPI.</summary>
    public static class EndpointDocs
    {
        /// <summary>Endpoint cần đăng nhập: có thể trả 401 (chưa đăng nhập / token hết hạn).</summary>
        public static TBuilder RequiresLogin<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
            builder.WithMetadata(new Microsoft.AspNetCore.Http.ProducesResponseTypeMetadata(
                StatusCodes.Status401Unauthorized, typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), ["application/problem+json"]));

        /// <summary>Endpoint chỉ dành cho admin: có thể trả 401 hoặc 403.</summary>
        public static TBuilder RequiresAdmin<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder =>
            builder.RequiresLogin().WithMetadata(new Microsoft.AspNetCore.Http.ProducesResponseTypeMetadata(
                StatusCodes.Status403Forbidden, typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), ["application/problem+json"]));
    }
}
