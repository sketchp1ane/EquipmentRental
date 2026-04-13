using EquipmentRental.Constants;
using EquipmentRental.Models;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using EquipmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Controllers;

[Authorize]
public class EquipmentController(
    EquipmentService equipmentService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private string CurrentUserId => userManager.GetUserId(User)!;

    // ── Equipment List ────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        int? categoryId, EquipmentStatus? status, string? keyword, int page = 1)
    {
        var vm = await equipmentService.GetPagedEquipmentsAsync(
            categoryId, status, keyword, page);
        return View(vm);
    }

    // ── Equipment Create ──────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.Admin},{Roles.DeviceAdmin}")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = await equipmentService.GetCreateViewModelAsync();
        return View(vm);
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.DeviceAdmin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateEquipmentViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.CategoryOptions = await equipmentService.GetCategorySelectListAsync();
            return View(vm);
        }

        var (success, errors) = await equipmentService.CreateEquipmentAsync(vm, CurrentUserId);
        if (!success)
        {
            foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
            vm.CategoryOptions = await equipmentService.GetCategorySelectListAsync();
            return View(vm);
        }

        TempData["SuccessMessage"] = "设备入库成功";
        return RedirectToAction(nameof(Index));
    }

    // ── Equipment Edit ────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.Admin},{Roles.DeviceAdmin}")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await equipmentService.GetEquipmentForEditAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.DeviceAdmin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditEquipmentViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.CategoryOptions  = await equipmentService.GetCategorySelectListAsync();
            vm.ExistingImages   = (await equipmentService.GetEquipmentForEditAsync(vm.Id))
                                      ?.ExistingImages ?? [];
            return View(vm);
        }

        var (success, errors) = await equipmentService.UpdateEquipmentAsync(vm, CurrentUserId);
        if (!success)
        {
            foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
            vm.CategoryOptions  = await equipmentService.GetCategorySelectListAsync();
            vm.ExistingImages   = (await equipmentService.GetEquipmentForEditAsync(vm.Id))
                                      ?.ExistingImages ?? [];
            return View(vm);
        }

        TempData["SuccessMessage"] = "设备信息已更新";
        return RedirectToAction(nameof(Details), new { id = vm.Id });
    }

    // ── Equipment Details ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vm = await equipmentService.GetEquipmentDetailsAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // ── Delete Image ──────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.Admin},{Roles.DeviceAdmin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var (success, equipmentId, error) =
            await equipmentService.DeleteImageAsync(id, CurrentUserId);

        if (!success)
            TempData["ErrorMessage"] = error;

        return RedirectToAction(nameof(Edit), new { id = equipmentId });
    }

    // ── Excel Export ──────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.Admin},{Roles.DeviceAdmin},{Roles.Auditor}")]
    [HttpGet]
    public async Task<IActionResult> Export(
        int? categoryId, EquipmentStatus? status, string? keyword)
    {
        var bytes = await equipmentService.ExportToExcelAsync(categoryId, status, keyword);
        var fileName = $"设备台账_{DateTime.Now:yyyyMMddHHmm}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    // ── Category List ─────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.Admin},{Roles.DeviceAdmin}")]
    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var items = await equipmentService.GetCategoryListAsync();
        var vm = new CategoryListViewModel
        {
            Items = items,
            Form  = new CategoryFormViewModel
            {
                ParentOptions = await equipmentService.GetCategorySelectListAsync()
            }
        };
        return View(vm);
    }

    // ── Category Create ───────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.Admin},{Roles.DeviceAdmin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CategoryFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var items = await equipmentService.GetCategoryListAsync();
            vm.ParentOptions = await equipmentService.GetCategorySelectListAsync();
            return View("Categories", new CategoryListViewModel { Items = items, Form = vm });
        }

        var (success, error) = await equipmentService.CreateCategoryAsync(vm, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            var items = await equipmentService.GetCategoryListAsync();
            vm.ParentOptions = await equipmentService.GetCategorySelectListAsync();
            return View("Categories", new CategoryListViewModel { Items = items, Form = vm });
        }

        TempData["SuccessMessage"] = "分类创建成功";
        return RedirectToAction(nameof(Categories));
    }

    // ── Category Edit ─────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.Admin},{Roles.DeviceAdmin}")]
    [HttpGet]
    public async Task<IActionResult> EditCategory(int id)
    {
        var vm = await equipmentService.GetCategoryForEditAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.DeviceAdmin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(CategoryFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.ParentOptions = await equipmentService.GetCategorySelectListAsync();
            return View(vm);
        }

        var (success, error) = await equipmentService.UpdateCategoryAsync(vm, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            vm.ParentOptions = await equipmentService.GetCategorySelectListAsync();
            return View(vm);
        }

        TempData["SuccessMessage"] = "分类已更新";
        return RedirectToAction(nameof(Categories));
    }

    // ── Category Delete ───────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.Admin},{Roles.DeviceAdmin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var (success, error) = await equipmentService.DeleteCategoryAsync(id, CurrentUserId);

        if (!success)
            TempData["ErrorMessage"] = error;
        else
            TempData["SuccessMessage"] = "分类已删除";

        return RedirectToAction(nameof(Categories));
    }
}
