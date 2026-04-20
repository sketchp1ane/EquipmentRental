using EquipmentRental.Constants;
using EquipmentRental.Models;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using EquipmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Controllers;

[Authorize(Roles = $"{Roles.Admin},{Roles.DeviceAdmin}")]
public class AuditController(
    AuditService auditService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private string CurrentUserId => userManager.GetUserId(User)!;

    [HttpGet]
    public async Task<IActionResult> Index(string? keyword, int page = 1)
    {
        var vm = await auditService.GetPendingListAsync(keyword, page);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Review(int equipmentId)
    {
        var vm = await auditService.GetDetailAsync(equipmentId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(AuditActionViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var detail = await auditService.GetDetailAsync(form.EquipmentId);
            if (detail == null) return NotFound();
            detail.Form = form;
            return View(detail);
        }

        if (form.Action == AuditAction.Reject && string.IsNullOrWhiteSpace(form.Remark))
        {
            ModelState.AddModelError("Form.Remark", "驳回时必须填写原因");
            var detail = await auditService.GetDetailAsync(form.EquipmentId);
            if (detail == null) return NotFound();
            detail.Form = form;
            return View(detail);
        }

        var (success, error) = form.Action == AuditAction.Pass
            ? await auditService.PassAsync(form.EquipmentId, CurrentUserId, form.Remark)
            : await auditService.RejectAsync(form.EquipmentId, CurrentUserId, form.Remark!);

        if (!success)
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(Review), new { equipmentId = form.EquipmentId });
        }

        TempData["SuccessMessage"] = form.Action == AuditAction.Pass ? "审核已通过" : "已驳回该设备";
        return RedirectToAction(nameof(Index));
    }
}
