using EquipmentRental.Constants;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using EquipmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Controllers;

[Authorize]
public class VerificationController(
    VerificationService verificationService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private string CurrentUserId => userManager.GetUserId(User)!;

    // ── 核验操作页 ────────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.ProjectLead},{Roles.Admin}")]
    [HttpGet]
    public async Task<IActionResult> Verify(string? code)
    {
        var vm = await verificationService.GetVerifyFormAsync(code);
        return View(vm);
    }

    [Authorize(Roles = $"{Roles.ProjectLead},{Roles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(VerifyFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            // 若已有核验码，重新加载订单预览
            if (!string.IsNullOrWhiteSpace(vm.VerifyCode))
            {
                var refreshed = await verificationService.GetVerifyFormAsync(vm.VerifyCode);
                refreshed.VerifyCode = vm.VerifyCode;
                return View(refreshed);
            }
            return View(vm);
        }

        var (success, isPass, failReason, verificationId) =
            await verificationService.PerformVerifyAsync(vm.VerifyCode!, CurrentUserId);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, failReason ?? "核验失败，请检查核验码。");
            return View(vm);
        }

        return RedirectToAction(nameof(Details), new { id = verificationId });
    }

    // ── 核验结果详情 ──────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.ProjectLead},{Roles.Admin},{Roles.Dispatcher},{Roles.Auditor}")]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vm = await verificationService.GetVerificationDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // ── 核验列表 ──────────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.Admin},{Roles.Auditor},{Roles.ProjectLead},{Roles.Dispatcher}")]
    [HttpGet]
    public async Task<IActionResult> List(int page = 1)
    {
        var vm = await verificationService.GetVerificationListAsync(page);
        return View(vm);
    }
}
