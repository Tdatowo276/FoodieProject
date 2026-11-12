using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FoodieAPI.Common
{
    /// <summary>
    /// Tài liệu REST API (Swagger/OpenAPI) tại /swagger.
    /// Bấm "Authorize", dán JWT lấy từ POST /api/auth/login để thử các API cần đăng nhập.
    /// </summary>
    public static class SwaggerSetup
    {
        public const string BearerScheme = "Bearer";

        public static IServiceCollection AddFoodieSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Foodie API",
                    Version = "v1",
                    Description =
                        "REST API cho ứng dụng đặt đồ ăn Foodie (React).\n\n" +
                        "**Xác thực:** gọi `POST /api/auth/login` để lấy token, bấm **Authorize** và dán token " +
                        "(không cần gõ chữ Bearer). Tài khoản admin dùng được cả các API `/api/admin/*`.\n\n" +
                        "**Lỗi** trả về dạng `application/problem+json` (ProblemDetails); lỗi dữ liệu có thêm " +
                        "`errors` theo từng trường."
                });

                options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT lấy từ POST /api/auth/login hoặc /api/auth/register."
                });

                // Chỉ gắn ổ khóa cho endpoint thật sự yêu cầu đăng nhập
                options.OperationFilter<AuthorizeOperationFilter>();

                // Chú thích /// <summary> của DTO hiển thị trong phần Schemas
                var xml = Path.Combine(AppContext.BaseDirectory, $"{typeof(SwaggerSetup).Assembly.GetName().Name}.xml");
                if (File.Exists(xml)) options.IncludeXmlComments(xml);
            });
            return services;
        }

        private sealed class AuthorizeOperationFilter : IOperationFilter
        {
            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
                var requiresAuth = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();
                if (!requiresAuth) return;

                operation.Security ??= [];
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(BearerScheme, context.Document)] = []
                });
            }
        }
    }
}
