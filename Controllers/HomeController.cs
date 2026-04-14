using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EquipmentRental.Models;
using EquipmentRental.Models.ViewModels;
using EquipmentRental.Services;

namespace EquipmentRental.Controllers;

[Authorize]
public class HomeController(QualificationService qualificationService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var vm = new HomeIndexViewModel
        {
            ExpiringCerts = await qualificationService.GetExpiringAsync(30)
        };
        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
