using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EquipmentRental.Models.ViewModels;
using EquipmentRental.Services;

namespace EquipmentRental.Controllers;

[Authorize]
public class ReportController(ReportService reportService) : Controller
{
    // ── Rental Statistics ─────────────────────────────────────────────────

    public async Task<IActionResult> Rental(DateOnly? from, DateOnly? to)
    {
        var today    = DateOnly.FromDateTime(DateTime.Today);
        var resolved = (From: from ?? today.AddMonths(-3), To: to ?? today);

        var vm = new RentalReportViewModel
        {
            From  = resolved.From,
            To    = resolved.To,
            Stats = await reportService.GetRentalStatsAsync(resolved.From, resolved.To)
        };
        return View(vm);
    }

    public async Task<IActionResult> ExportRentalExcel(DateOnly from, DateOnly to)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var bytes  = await reportService.ExportRentalExcelAsync(from, to, userId);
        var filename = $"租赁统计_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            filename);
    }

    // ── Utilization ───────────────────────────────────────────────────────

    public async Task<IActionResult> Utilization(DateOnly? from, DateOnly? to)
    {
        var today    = DateOnly.FromDateTime(DateTime.Today);
        var resolved = (From: from ?? today.AddMonths(-3), To: to ?? today);

        var vm = new UtilizationViewModel
        {
            From  = resolved.From,
            To    = resolved.To,
            Stats = await reportService.GetUtilizationAsync(resolved.From, resolved.To)
        };
        return View(vm);
    }

    // ── Safety Records ────────────────────────────────────────────────────

    public async Task<IActionResult> Safety(DateOnly? from, DateOnly? to)
    {
        var today    = DateOnly.FromDateTime(DateTime.Today);
        var resolved = (From: from ?? today.AddMonths(-3), To: to ?? today);

        var vm = new SafetyReportViewModel
        {
            From  = resolved.From,
            To    = resolved.To,
            Stats = await reportService.GetSafetyStatsAsync(resolved.From, resolved.To)
        };
        return View(vm);
    }
}
