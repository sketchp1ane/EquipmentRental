using EquipmentRental.Constants;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using EquipmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Controllers;

[Authorize]
public class SafetyController(
    SafetyService safetyService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private string CurrentUserId => userManager.GetUserId(User)!;

    // ── 安全交底列表 ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> List(int page = 1)
    {
        var vm = await safetyService.GetListAsync(page);
        return View(vm);
    }

    // ── 创建安全交底 ──────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.SafetyOfficer},{Roles.Admin}")]
    [HttpGet]
    public async Task<IActionResult> Create(int orderId)
    {
        var vm = await safetyService.GetCreateFormAsync(orderId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = $"{Roles.SafetyOfficer},{Roles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBriefingViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            // 保留展示字段
            var form = await safetyService.GetCreateFormAsync(vm.OrderId);
            if (form != null) { vm.ProjectName = form.ProjectName; vm.EquipmentNo = form.EquipmentNo; }
            return View(vm);
        }

        var (success, error, id) = await safetyService.CreateBriefingAsync(vm, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(vm);
        }

        TempData["SuccessMessage"] = "安全交底记录已创建。";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── 详情 + 签署 ───────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vm = await safetyService.GetDetailAsync(id, CurrentUserId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = $"{Roles.SafetyOfficer},{Roles.ProjectLead},{Roles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sign(int id)
    {
        var (success, error) = await safetyService.SignAsync(id, CurrentUserId);
        if (!success)
        {
            TempData["ErrorMessage"] = error;
        }
        else
        {
            TempData["SuccessMessage"] = "签署成功。";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── PDF 导出 ──────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> ExportPdf(int id)
    {
        var pdf = await safetyService.ExportPdfAsync(id);
        if (pdf == null) return NotFound();
        return File(pdf, "application/pdf", $"safety-briefing-{id}.pdf");
    }
}
