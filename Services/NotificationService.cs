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

    public async Task<IList<Notification>> GetRecentAsync(string userId, int take = 10)
    {
        return await _db.Notifications
            .Where(n => n.RecipientId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<bool> MarkReadAsync(int id, string userId)
    {
        var n = await _db.Notifications
            .FirstOrDefaultAsync(x => x.Id == id && x.RecipientId == userId);
        if (n == null) return false;
        if (!n.IsRead)
        {
            n.IsRead = true;
            await _db.SaveChangesAsync();
        }
        return true;
    }

    public async Task<int> MarkAllReadAsync(string userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync();
        return unread.Count;
    }
}
