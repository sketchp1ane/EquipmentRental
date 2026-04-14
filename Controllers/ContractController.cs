using EquipmentRental.Constants;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using EquipmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Controllers;

[Authorize]
public class ContractController(
    DispatchService dispatchService,
    FileService fileService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private string CurrentUserId => userManager.GetUserId(User)!;

    private const string AllViewRoles =
        $"{Roles.Dispatcher},{Roles.ProjectLead},{Roles.Admin},{Roles.Auditor},{Roles.DeviceAdmin}";

    // ── 合同详情 / 在线预览 ────────────────────────────────────────────────

    [Authorize(Roles = AllViewRoles)]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vm = await dispatchService.GetContractDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // ── 合同 PDF 导出 ──────────────────────────────────────────────────────

    [Authorize(Roles = AllViewRoles)]
    [HttpGet]
    public async Task<IActionResult> ExportPdf(int id)
    {
        var pdf = await dispatchService.ExportContractPdfAsync(id);
        if (pdf == null) return NotFound();
        return File(pdf, "application/pdf", $"contract-{id}.pdf");
    }

    // ── 上传扫描件 ─────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.Dispatcher},{Roles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadScan(UploadScanViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "请选择要上传的文件。";
            return RedirectToAction(nameof(Details), new { id = vm.ContractId });
        }

        var (success, error) = await dispatchService.UploadScanAsync(
            vm.ContractId, vm.ScanFile, CurrentUserId, fileService);

        if (!success)
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(Details), new { id = vm.ContractId });
        }

        TempData["SuccessMessage"] = "扫描件已上传，合同状态已更新为已签署。";
        return RedirectToAction(nameof(Details), new { id = vm.ContractId });
    }
}
