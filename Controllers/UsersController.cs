using EquipmentRental.Constants;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using EquipmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Controllers;

[Authorize(Roles = $"{Roles.Admin},{Roles.Auditor}")]
public class UsersController(
    UserService userService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private string CurrentUserId => userManager.GetUserId(User)!;

    private bool IsAuditorOnly =>
        User.IsInRole(Roles.Auditor) && !User.IsInRole(Roles.Admin);

    // ── Index ─────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        string? keyword, string? role, bool? isActive, int page = 1)
    {
        var vm = await userService.GetPagedUsersAsync(keyword, role, isActive, page);
        ViewBag.IsReadOnly = IsAuditorOnly;
        return View(vm);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public IActionResult Create()
    {
        var vm = new CreateUserViewModel { AllRoles = Roles.All };
        return View(vm);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AllRoles = Roles.All;
            return View(vm);
        }

        var (success, errors) = await userService.CreateUserAsync(vm, CurrentUserId);
        if (!success)
        {
            foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
            vm.AllRoles = Roles.All;
            return View(vm);
        }

        TempData["SuccessMessage"] = "用户创建成功";
        return RedirectToAction(nameof(Index));
    }

    // ── Edit ──────────────────────────────────────────────────────────────────

    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var vm = await userService.GetUserForEditAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AllRoles = Roles.All;
            return View(vm);
        }

        var (success, errors) = await userService.UpdateUserAsync(vm, CurrentUserId);
        if (!success)
        {
            foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
            vm.AllRoles = Roles.All;
            return View(vm);
        }

        TempData["SuccessMessage"] = "用户信息已更新";
        return RedirectToAction(nameof(Index));
    }

    // ── ToggleActive ──────────────────────────────────────────────────────────

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var (success, newIsActive, error) = await userService.ToggleActiveAsync(id, CurrentUserId);

        if (Request.Headers.XRequestedWith == "XMLHttpRequest")
        {
            if (!success)
                return BadRequest(new { message = error });
            return Json(new { isActive = newIsActive });
        }

        if (!success)
            TempData["ErrorMessage"] = error;
        else
            TempData["SuccessMessage"] = newIsActive ? "用户已启用" : "用户已停用";

        return RedirectToAction(nameof(Index));
    }

    // ── ResetPassword ─────────────────────────────────────────────────────────

    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var vm = await userService.GetResetPasswordViewModelAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var (success, errors) = await userService.ResetPasswordAsync(
            vm.UserId, vm.NewPassword, CurrentUserId);

        if (!success)
        {
            foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
            return View(vm);
        }

        TempData["SuccessMessage"] = $"已成功重置 {vm.UserName} 的密码";
        return RedirectToAction(nameof(Index));
    }
}
