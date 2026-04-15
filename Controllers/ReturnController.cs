using EquipmentRental.Constants;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using EquipmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Controllers;

[Authorize]
public class ReturnController(
    ReturnService returnService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private string CurrentUserId => userManager.GetUserId(User)!;

    // ── 退场申请列表 ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        // ProjectLead (when not also Admin/DeviceAdmin/Auditor) only sees their own applications
        bool showAll = User.IsInRole(Roles.Admin)
                    || User.IsInRole(Roles.DeviceAdmin)
                    || User.IsInRole(Roles.Auditor)
                    || User.IsInRole(Roles.Dispatcher)
                    || User.IsInRole(Roles.SafetyOfficer);
        string? restrictToUserId = showAll ? null : CurrentUserId;
        var vm = await returnService.GetListAsync(restrictToUserId, page);
        return View(vm);
    }

    // ── 退场申请 ──────────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.ProjectLead},{Roles.Admin}")]
    [HttpGet]
    public async Task<IActionResult> Apply(int orderId)
    {
        var vm = await returnService.GetApplyFormAsync(orderId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = $"{Roles.ProjectLead},{Roles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(ApplyReturnViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await returnService.GetApplyFormAsync(vm.OrderId);
            if (form != null) { vm.ProjectName = form.ProjectName; vm.EquipmentNo = form.EquipmentNo; }
            return View(vm);
        }

        var (success, error, id) = await returnService.CreateApplicationAsync(vm, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            var form = await returnService.GetApplyFormAsync(vm.OrderId);
            if (form != null) { vm.ProjectName = form.ProjectName; vm.EquipmentNo = form.EquipmentNo; }
            return View(vm);
        }

        TempData["SuccessMessage"] = "退场申请已提交，等待设备管理员评价。";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── 退场申请详情 ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vm = await returnService.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // ── 填写退场评价 ──────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.DeviceAdmin},{Roles.Admin}")]
    [HttpGet]
    public async Task<IActionResult> Evaluate(int id)
    {
        var vm = await returnService.GetEvaluateFormAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = $"{Roles.DeviceAdmin},{Roles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Evaluate(EvaluateReturnViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await returnService.GetEvaluateFormAsync(vm.ReturnAppId);
            if (form != null) { vm.ProjectName = form.ProjectName; vm.EquipmentNo = form.EquipmentNo; vm.Deposit = form.Deposit; }
            return View(vm);
        }

        var (success, error) = await returnService.CreateEvaluationAsync(vm, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            var form = await returnService.GetEvaluateFormAsync(vm.ReturnAppId);
            if (form != null) { vm.ProjectName = form.ProjectName; vm.EquipmentNo = form.EquipmentNo; vm.Deposit = form.Deposit; }
            return View(vm);
        }

        TempData["SuccessMessage"] = "退场评价已提交，设备状态已更新。";
        return RedirectToAction(nameof(Details), new { id = vm.ReturnAppId });
    }
}
