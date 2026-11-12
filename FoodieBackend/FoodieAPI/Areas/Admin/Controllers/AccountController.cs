using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using FoodieAPI.Areas.Admin.Models;
using FoodieAPI.Common;
using FoodieAPI.Services;

namespace FoodieAPI.Areas.Admin.Controllers
{
    public class AccountController(IAuthService auth, ILogger<AccountController> logger) : AdminControllerBase
    {
        [HttpGet, AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.IsInRole(Roles.Admin))
                return RedirectToLocal(returnUrl);

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, AllowAnonymous]
        [EnableRateLimiting("auth")]    // tối đa 10 lần/phút/IP, chống dò mật khẩu
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var user = await auth.ValidateCredentialsAsync(model.Email, model.Password);

                // Khách hàng đúng mật khẩu vẫn không được vào khu quản trị
                if (user.Role != Roles.Admin)
                {
                    logger.LogWarning("Tài khoản {UserId} (vai trò {Role}) cố đăng nhập khu Admin", user.Id, user.Role);
                    ModelState.AddModelError(string.Empty, "Tài khoản này không có quyền quản trị.");
                    return View(model);
                }

                var identity = new ClaimsIdentity(
                    auth.BuildClaims(user), CookieAuthenticationDefaults.AuthenticationScheme, "name", "role");

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = model.RememberMe });

                FlashSuccess($"Xin chào {user.Username}!");
                return RedirectToLocal(model.ReturnUrl);
            }
            catch (ApiException ex)
            {
                AddErrors(ex);
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            logger.LogInformation("Người dùng {Name} đăng xuất khu Admin", User.Identity?.Name);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet, AllowAnonymous]
        public IActionResult AccessDenied() => View();

        // Chỉ chuyển hướng tới URL nội bộ, chặn lỗ hổng Open Redirect (?returnUrl=https://trang-lua-dao)
        private IActionResult RedirectToLocal(string? returnUrl) =>
            Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl!)
                : RedirectToAction("Index", "Dashboard");
    }
}
