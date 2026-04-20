using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EquipmentRental.Models;
using EquipmentRental.Models.ViewModels;
using EquipmentRental.Services;

namespace EquipmentRental.Controllers;

[Authorize]
public class HomeController(
    QualificationService qualificationService,
    DashboardService dashboardService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var vm = new HomeIndexViewModel
        {
            ExpiringCerts  = await qualificationService.GetExpiringAsync(30),
            Stats          = await dashboardService.GetDashboardStatsAsync(),
            PendingActions = await dashboardService.GetPendingActionsAsync(User)
        };
        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? id)
    {
        var statusCode = id ?? 500;
        Response.StatusCode = statusCode;
        return View(new ErrorViewModel
        {
            RequestId  = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = statusCode
        });
    }
}
