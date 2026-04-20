using EquipmentRental.Models.Entities;
using EquipmentRental.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EquipmentRental.Controllers;

[Authorize]
[Route("Notification")]
public class NotificationController(
    NotificationService notificationService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private string CurrentUserId => userManager.GetUserId(User)!;

    [HttpGet("Recent")]
    public async Task<IActionResult> Recent()
    {
        var unread = await notificationService.GetUnreadCountAsync(CurrentUserId);
        var items = await notificationService.GetRecentAsync(CurrentUserId, 10);
        return Json(new
        {
            unread,
            items = items.Select(n => new
            {
                id         = n.Id,
                title      = n.Title,
                content    = n.Content,
                isRead     = n.IsRead,
                relatedUrl = n.RelatedUrl,
                createdAt  = n.CreatedAt.ToLocalTime().ToString("MM-dd HH:mm")
            })
        });
    }

    [HttpPost("MarkRead/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var ok = await notificationService.MarkReadAsync(id, CurrentUserId);
        return ok ? Ok() : NotFound();
    }

    [HttpPost("MarkAllRead")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var count = await notificationService.MarkAllReadAsync(CurrentUserId);
        return Json(new { count });
    }
}
