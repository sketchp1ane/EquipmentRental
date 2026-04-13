using EquipmentRental.Data;
using EquipmentRental.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Services;

public class NotificationService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task SendAsync(string recipientId, string title, string content, string? relatedUrl = null)
    {
        _db.Notifications.Add(new Notification
        {
            RecipientId = recipientId,
            Title = title,
            Content = content,
            RelatedUrl = relatedUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _db.Notifications
            .CountAsync(n => n.RecipientId == userId && !n.IsRead);
    }
}
