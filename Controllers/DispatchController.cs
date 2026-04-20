using EquipmentRental.Constants;
using EquipmentRental.Models;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using EquipmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace EquipmentRental.Controllers;

[Authorize]
public class DispatchController(
    DispatchService dispatchService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private string CurrentUserId => userManager.GetUserId(User)!;

    // ── 用车申请（项目负责人）────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.ProjectLead},{Roles.Admin}")]
    [HttpGet]
    [ActionName("Request")]
    public async Task<IActionResult> DispatchRequest()
    {
        var vm = await dispatchService.GetRequestFormAsync();
        return View("Request", vm);
    }

    [Authorize(Roles = $"{Roles.ProjectLead},{Roles.Admin}")]
    [HttpPost]
    [ActionName("Request")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DispatchRequestPost(DispatchRequestFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.CategoryOptions = await dispatchService.GetCategorySelectListAsync();
            return View(vm);
        }

        var (success, error) = await dispatchService.SubmitRequestAsync(vm, CurrentUserId);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            vm.CategoryOptions = await dispatchService.GetCategorySelectListAsync();
            return View(vm);
        }

        TempData["SuccessMessage"] = "用车申请已提交，等待调度员安排。排期完成后可在「调度单」列表查看。";
        return RedirectToAction(nameof(Orders));
    }

    // ── 调度列表（调度员 / 管理员 / 审计员）─────────────────────────────────

    [Authorize(Roles = $"{Roles.Dispatcher},{Roles.Admin},{Roles.Auditor}")]
    [HttpGet]
    public async Task<IActionResult> Index(DispatchRequestStatus? status, int page = 1)
    {
        var vm = await dispatchService.GetRequestListAsync(status, page);
        return View(vm);
    }

    // ── 调度单列表（所有相关角色）────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.Dispatcher},{Roles.Admin},{Roles.ProjectLead},{Roles.Auditor}")]
    [HttpGet]
    public async Task<IActionResult> Orders(DispatchOrderStatus? status, string? keyword, int page = 1)
    {
        // 项目负责人只看自己提交的申请衍生的调度单
        var restrictToRequesterId = User.IsInRole(Roles.ProjectLead)
            && !User.IsInRole(Roles.Admin)
            && !User.IsInRole(Roles.Dispatcher)
            && !User.IsInRole(Roles.Auditor)
            ? CurrentUserId
            : null;

        var vm = await dispatchService.GetOrderListAsync(status, keyword, restrictToRequesterId, page);
        return View(vm);
    }

    // ── 调度排期(调度员)────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.Dispatcher},{Roles.Admin}")]
    [HttpGet]
    public async Task<IActionResult> Order(int requestId, DateOnly? start, DateOnly? end)
    {
        var s = start ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var e = end   ?? DateOnly.FromDateTime(DateTime.Today.AddDays(8));

        var vm = await dispatchService.GetCreateOrderViewModelAsync(requestId, s, e);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [Authorize(Roles = $"{Roles.Dispatcher},{Roles.Admin}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Order(CreateOrderViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var refreshed = await dispatchService.GetCreateOrderViewModelAsync(
                vm.RequestId, vm.ActualStart, vm.ActualEnd);
            if (refreshed == null) return NotFound();
            refreshed.EquipmentId = vm.EquipmentId;
            refreshed.ActualStart = vm.ActualStart;
            refreshed.ActualEnd   = vm.ActualEnd;
            refreshed.UnitPrice   = vm.UnitPrice;
            refreshed.Deposit     = vm.Deposit;
            return View(refreshed);
        }

        var (success, error, orderId) = await dispatchService.CreateOrderAsync(
            vm.RequestId, vm.EquipmentId,
            vm.ActualStart, vm.ActualEnd,
            vm.UnitPrice, vm.Deposit,
            CurrentUserId);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            var refreshed = await dispatchService.GetCreateOrderViewModelAsync(
                vm.RequestId, vm.ActualStart, vm.ActualEnd);
            if (refreshed == null) return NotFound();
            refreshed.EquipmentId = vm.EquipmentId;
            refreshed.ActualStart = vm.ActualStart;
            refreshed.ActualEnd   = vm.ActualEnd;
            refreshed.UnitPrice   = vm.UnitPrice;
            refreshed.Deposit     = vm.Deposit;
            return View(refreshed);
        }

        TempData["SuccessMessage"] = "调度单已创建，合同草稿已自动生成。";
        return RedirectToAction(nameof(OrderDetails), new { id = orderId });
    }

    // ── 调度单详情 ────────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.Dispatcher},{Roles.Admin},{Roles.ProjectLead},{Roles.Auditor}")]
    [HttpGet]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var vm = await dispatchService.GetOrderDetailAsync(id);
        if (vm == null) return NotFound();

        // 生成二维码（VerifyCode 直接作为文本内容）
        using var qrGen = new QRCodeGenerator();
        var qrData = qrGen.CreateQrCode(vm.VerifyCode, QRCodeGenerator.ECCLevel.M);
        using var pngCode = new PngByteQRCode(qrData);
        var png = pngCode.GetGraphic(5);
        vm.QrCodeDataUri = "data:image/png;base64," + Convert.ToBase64String(png);

        return View(vm);
    }

    // ── 调度日历 ──────────────────────────────────────────────────────────────

    [Authorize(Roles = $"{Roles.Dispatcher},{Roles.Admin}")]
    [HttpGet]
    public IActionResult Calendar(DateOnly? start, DateOnly? end)
    {
        var vm = new CalendarPageViewModel
        {
            RangeStart = start ?? DateOnly.FromDateTime(DateTime.Today),
            RangeEnd   = end   ?? DateOnly.FromDateTime(DateTime.Today.AddDays(29))
        };
        return View(vm);
    }

    [Authorize(Roles = $"{Roles.Dispatcher},{Roles.Admin}")]
    [HttpGet]
    public async Task<IActionResult> CalendarData(DateOnly start, DateOnly end)
    {
        var data = await dispatchService.GetCalendarDataAsync(start, end);
        return Json(data);
    }
}
