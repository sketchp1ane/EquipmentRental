using EquipmentRental.Constants;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using EquipmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Controllers;

[Authorize(Roles = $"{Roles.Admin},{Roles.DeviceAdmin}")]
public class QualificationController(
    QualificationService qualificationService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private string CurrentUserId => userManager.GetUserId(User)!;

    [HttpGet]
    public async Task<IActionResult> Index(int equipmentId)
    {
        var vm = await qualificationService.GetIndexAsync(equipmentId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int equipmentId)
    {
        var vm = await qualificationService.GetCreateViewModelAsync(equipmentId);
        if (vm.EquipmentName == string.Empty && vm.EquipmentNo == string.Empty)
            return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QualificationFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        if (vm.ValidTo < vm.ValidFrom)
        {
            ModelState.AddModelError(nameof(vm.ValidTo), "有效期截止日期不能早于起始日期");
            return View(vm);
        }

        var (success, error) = await qualificationService.CreateAsync(vm, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(vm);
        }

        TempData["SuccessMessage"] = "证件添加成功";
        return RedirectToAction(nameof(Index), new { equipmentId = vm.EquipmentId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await qualificationService.GetForEditAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(QualificationFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        if (vm.ValidTo < vm.ValidFrom)
        {
            ModelState.AddModelError(nameof(vm.ValidTo), "有效期截止日期不能早于起始日期");
            return View(vm);
        }

        var (success, error) = await qualificationService.UpdateAsync(vm, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(vm);
        }

        TempData["SuccessMessage"] = "证件更新成功";
        return RedirectToAction(nameof(Index), new { equipmentId = vm.EquipmentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int equipmentId)
    {
        var (success, returnedEquipmentId, error) = await qualificationService.DeleteAsync(id, CurrentUserId);
        if (!success)
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(Index), new { equipmentId });
        }

        TempData["SuccessMessage"] = "证件已删除";
        return RedirectToAction(nameof(Index), new { equipmentId = returnedEquipmentId });
    }
}
