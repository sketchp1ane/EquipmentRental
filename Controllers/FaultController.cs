using EquipmentRental.Constants;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using EquipmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Controllers;

[Authorize]
public class FaultController(
    FaultService faultService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private string CurrentUserId => userManager.GetUserId(User)!;

    // ── 故障工单列表 ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var vm = await faultService.GetListAsync(page);
        return View(vm);
    }

    // ── 故障上报 ──────────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.SafetyOfficer},{Roles.ProjectLead},{Roles.Admin}")]
    [HttpGet]
    public async Task<IActionResult> Create(int orderId)
    {
        var vm = await faultService.GetCreateFormAsync(orderId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = $"{Roles.SafetyOfficer},{Roles.ProjectLead},{Roles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateFaultViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await faultService.GetCreateFormAsync(vm.OrderId);
            if (form != null) { vm.ProjectName = form.ProjectName; vm.EquipmentNo = form.EquipmentNo; }
            return View(vm);
        }

        var (success, error, id) = await faultService.CreateFaultAsync(vm, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            var form = await faultService.GetCreateFormAsync(vm.OrderId);
            if (form != null) { vm.ProjectName = form.ProjectName; vm.EquipmentNo = form.EquipmentNo; }
            return View(vm);
        }

        TempData["SuccessMessage"] = "故障已上报，设备已标记为维修中。";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── 故障工单详情 ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vm = await faultService.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // ── 受理工单 ──────────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.DeviceAdmin},{Roles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int id)
    {
        var (success, error) = await faultService.AcceptFaultAsync(id, CurrentUserId);
        if (!success)
            TempData["ErrorMessage"] = error;
        else
            TempData["SuccessMessage"] = "工单已受理，进入处理中状态。";

        return RedirectToAction(nameof(Details), new { id });
    }

    // ── 关闭工单 + 恢复设备 ───────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.DeviceAdmin},{Roles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(CloseFaultViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "请填写完整的处理说明和维修费用。";
            return RedirectToAction(nameof(Details), new { id = vm.Id });
        }

        var (success, error, restoredStatusText) = await faultService.CloseFaultAsync(vm, CurrentUserId);
        if (!success)
            TempData["ErrorMessage"] = error;
        else
            TempData["SuccessMessage"] = $"工单已关闭，设备已恢复为{restoredStatusText}状态。";

        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }
}
