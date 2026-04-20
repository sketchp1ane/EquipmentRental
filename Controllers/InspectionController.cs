using EquipmentRental.Constants;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using EquipmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Controllers;

[Authorize]
public class InspectionController(
    InspectionService inspectionService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private string CurrentUserId => userManager.GetUserId(User)!;

    // ── 巡检记录列表 ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var vm = await inspectionService.GetListAsync(page);
        return View(vm);
    }

    // ── 新建巡检记录 ──────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.SafetyOfficer},{Roles.Admin}")]
    [HttpGet]
    public async Task<IActionResult> Create(int orderId)
    {
        var vm = await inspectionService.GetCreateFormAsync(orderId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = $"{Roles.SafetyOfficer},{Roles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateInspectionViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var form = await inspectionService.GetCreateFormAsync(vm.OrderId);
            if (form != null) { vm.ProjectName = form.ProjectName; vm.EquipmentNo = form.EquipmentNo; }
            RepopulateItemMeta(vm);
            return View(vm);
        }

        var (success, error, id) = await inspectionService.CreateInspectionAsync(vm, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            var form = await inspectionService.GetCreateFormAsync(vm.OrderId);
            if (form != null) { vm.ProjectName = form.ProjectName; vm.EquipmentNo = form.EquipmentNo; }
            RepopulateItemMeta(vm);
            return View(vm);
        }

        TempData["SuccessMessage"] = "巡检记录已创建。";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── 巡检记录详情 ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vm = await inspectionService.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // Re-hydrate item names/order after model binding from the form.
    private static void RepopulateItemMeta(CreateInspectionViewModel vm)
    {
        foreach (var item in vm.Items)
        {
            var meta = Constants.InspectionChecklist.Standard.FirstOrDefault(i => i.Key == item.ItemKey);
            if (meta != null)
            {
                item.ItemName = meta.Name;
                item.Order    = meta.Order;
            }
        }
    }
}
