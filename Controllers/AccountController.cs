using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using EquipmentRental.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Controllers;

[Authorize]
public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    UserService userService) : Controller
{
    // ── Login ─────────────────────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(returnUrl ?? "/");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(vm);

        // Check IsActive before attempting sign-in
        var user = await userManager.FindByNameAsync(vm.Username);
        if (user != null && !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "账号已被停用，请联系管理员");
            return View(vm);
        }

        var result = await signInManager.PasswordSignInAsync(
            vm.Username,
            vm.Password,
            isPersistent: vm.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (vm.RememberMe)
            {
                // Re-issue cookie with 7-day absolute expiry
                await signInManager.SignOutAsync();
                user ??= await userManager.FindByNameAsync(vm.Username);
                await signInManager.SignInAsync(user!, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc   = DateTimeOffset.UtcNow.AddDays(7)
                });
            }

            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        }

        if (result.IsLockedOut)
        {
            if (user != null)
                await userService.LogLockoutEventAsync(user.Id,
                    HttpContext.Connection.RemoteIpAddress?.ToString());

            ModelState.AddModelError(string.Empty, "账号已被锁定，请30分钟后再试");
            return View(vm);
        }

        ModelState.AddModelError(string.Empty, "用户名或密码错误");
        return View(vm);
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    // ── Profile ───────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = userManager.GetUserId(User)!;
        var vm = await userService.GetProfileAsync(userId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var userId = userManager.GetUserId(User)!;
        var (success, errors) = await userService.UpdateProfileAsync(userId, vm);
        if (!success)
        {
            foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
            return View(vm);
        }

        TempData["SuccessMessage"] = "个人信息已更新";
        return RedirectToAction(nameof(Profile));
    }

    // ── Change Password ───────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var userId = userManager.GetUserId(User)!;
        var (success, errors) = await userService.ChangePasswordAsync(userId, vm);
        if (!success)
        {
            foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
            return View(vm);
        }

        TempData["SuccessMessage"] = "密码修改成功，请重新登录";
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    // ── Access Denied ─────────────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();
}
