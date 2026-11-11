using FoodieAPI.Common;
using FoodieAPI.DTOs;
using FoodieAPI.Services;

namespace FoodieAPI.Endpoints
{
    /// <summary>
    /// Web API hồ sơ cá nhân. Không có {id} trên URL: luôn là tài khoản trong token,
    /// nên không thể sửa hồ sơ của người khác (chống IDOR).
    /// </summary>
    public static class ProfileEndpoints
    {
        public static void MapProfile(this IEndpointRouteBuilder app)
        {
            var profile = app.MapGroup("/api/profile").WithTags("Hồ sơ cá nhân").RequireAuthorization().RequiresLogin();

            profile.MapGet("/", (IProfileService s, ICurrentUser me) => s.GetAsync(me.RequireId()))
                .WithSummary("Xem hồ sơ (họ tên, ảnh đại diện, địa chỉ mặc định)");

            profile.MapPut("/", (UpdateProfileRequest req, IProfileService s, ICurrentUser me) => s.UpdateAsync(me.RequireId(), req))
                .Validate<UpdateProfileRequest>()
                .WithSummary("Đổi họ tên")
                .ProducesValidationProblem();

            profile.MapPut("/password", async (ChangePasswordRequest req, IProfileService s, ICurrentUser me) =>
                {
                    await s.ChangePasswordAsync(me.RequireId(), req);
                    return Results.NoContent();
                })
                .Validate<ChangePasswordRequest>()
                .RequireRateLimiting("auth")   // chống dò mật khẩu hiện tại
                .WithSummary("Đổi mật khẩu (bắt buộc nhập đúng mật khẩu hiện tại)")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesValidationProblem()
                .ProducesProblem(StatusCodes.Status429TooManyRequests);

            // JWT trong header (không dùng cookie) nên không bị CSRF => tắt antiforgery cho form upload
            profile.MapPost("/avatar", (IFormFile? file, IProfileService s, ICurrentUser me, HttpContext ctx) =>
                    s.SetAvatarAsync(me.RequireId(), file, ctx.RequestAborted))
                .DisableAntiforgery()
                .WithSummary("Đổi ảnh đại diện (multipart/form-data, field \"file\")")
                .WithDescription("Chỉ nhận .jpg/.jpeg/.png tối đa 2MB; server kiểm tra nội dung thật của file.")
                .ProducesProblem(StatusCodes.Status400BadRequest);

            profile.MapDelete("/avatar", (IProfileService s, ICurrentUser me) => s.RemoveAvatarAsync(me.RequireId()))
                .WithSummary("Gỡ ảnh đại diện");

            profile.MapPut("/address", (AddressRequest req, IProfileService s, ICurrentUser me) =>
                    s.SetDefaultAddressAsync(me.RequireId(), req))
                .Validate<AddressRequest>()
                .WithSummary("Lưu địa chỉ giao hàng mặc định")
                .ProducesValidationProblem();

            profile.MapDelete("/address", (IProfileService s, ICurrentUser me) => s.ClearDefaultAddressAsync(me.RequireId()))
                .WithSummary("Xóa địa chỉ mặc định");
        }
    }
}
