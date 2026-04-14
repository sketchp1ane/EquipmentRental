using EquipmentRental.Data;
using EquipmentRental.Models;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Services;

public class AuditService(
    AppDbContext db,
    NotificationService notificationService,
    QualificationService qualificationService,
    IHttpContextAccessor httpContextAccessor)
{
    private string? ClientIp =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private async Task WriteOperationLogAsync(string actorId, string action, string entityId, string? detail = null)
    {
        db.OperationLogs.Add(new OperationLog
        {
            UserId     = actorId,
            Action     = action,
            EntityType = "Equipment",
            EntityId   = entityId,
            Detail     = detail,
            OccurredAt = DateTime.UtcNow,
            ClientIp   = ClientIp
        });
        await db.SaveChangesAsync();
    }

    public async Task<AuditListViewModel> GetPendingListAsync(string? keyword, int page, int pageSize = 15)
    {
        var query = db.Equipments
            .Include(e => e.Category)
            .Include(e => e.CreatedBy)
            .Where(e => e.Status == EquipmentStatus.PendingReview);

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(e =>
                e.EquipmentNo.Contains(keyword) ||
                e.Name.Contains(keyword));

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var equipments = await query
            .OrderBy(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var qualCounts = await db.Qualifications
            .Where(q => equipments.Select(e => e.Id).Contains(q.EquipmentId))
            .GroupBy(q => q.EquipmentId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var items = equipments.Select(e => new AuditListItemViewModel
        {
            EquipmentId       = e.Id,
            EquipmentNo       = e.EquipmentNo,
            EquipmentName     = e.Name,
            CategoryName      = e.Category.Name,
            CreatedByName     = e.CreatedBy.RealName,
            CreatedAt         = e.CreatedAt,
            QualificationCount = qualCounts.TryGetValue(e.Id, out var c) ? c : 0
        }).ToList();

        return new AuditListViewModel
        {
            Items      = items,
            Page       = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            Keyword    = keyword
        };
    }

    public async Task<AuditDetailViewModel?> GetDetailAsync(int equipmentId)
    {
        var equipment = await db.Equipments
            .Include(e => e.Category)
            .Include(e => e.CreatedBy)
            .FirstOrDefaultAsync(e => e.Id == equipmentId && e.Status == EquipmentStatus.PendingReview);

        if (equipment == null) return null;

        var qualifications = await db.Qualifications
            .Where(q => q.EquipmentId == equipmentId)
            .OrderBy(q => q.Type)
            .ToListAsync();

        return new AuditDetailViewModel
        {
            EquipmentId   = equipment.Id,
            EquipmentNo   = equipment.EquipmentNo,
            EquipmentName = equipment.Name,
            CategoryName  = equipment.Category.Name,
            BrandModel    = equipment.BrandModel,
            OwnedBy       = equipment.OwnedBy,
            CreatedByName = equipment.CreatedBy.RealName,
            CreatedAt     = equipment.CreatedAt,
            Qualifications = qualificationService.MapToListItems(qualifications),
            Form = new AuditActionViewModel { EquipmentId = equipmentId }
        };
    }

    public async Task<(bool Success, string? Error)> PassAsync(int equipmentId, string auditorId, string? remark)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var equipment = await db.Equipments
            .Include(e => e.CreatedBy)
            .FirstOrDefaultAsync(e => e.Id == equipmentId && e.Status == EquipmentStatus.PendingReview);

        if (equipment == null)
            return (false, "设备不存在或状态已变更，无法审核");

        equipment.Status = EquipmentStatus.Idle;

        db.AuditRecords.Add(new AuditRecord
        {
            EquipmentId = equipmentId,
            AuditorId   = auditorId,
            Action      = AuditAction.Pass,
            Remark      = remark,
            AuditedAt   = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        await notificationService.SendAsync(
            equipment.CreatedBy.Id,
            $"审核通过：{equipment.EquipmentNo}",
            $"您提交的设备【{equipment.EquipmentNo} {equipment.Name}】已通过审核，当前状态：闲置。",
            $"/Equipment/Details/{equipmentId}");

        await WriteOperationLogAsync(auditorId, "AuditPass", equipmentId.ToString(),
            remark);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RejectAsync(int equipmentId, string auditorId, string remark)
    {
        if (string.IsNullOrWhiteSpace(remark))
            return (false, "驳回时必须填写原因");

        await using var tx = await db.Database.BeginTransactionAsync();

        var equipment = await db.Equipments
            .Include(e => e.CreatedBy)
            .FirstOrDefaultAsync(e => e.Id == equipmentId && e.Status == EquipmentStatus.PendingReview);

        if (equipment == null)
            return (false, "设备不存在或状态已变更，无法审核");

        // Status stays PendingReview
        db.AuditRecords.Add(new AuditRecord
        {
            EquipmentId = equipmentId,
            AuditorId   = auditorId,
            Action      = AuditAction.Reject,
            Remark      = remark,
            AuditedAt   = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        await notificationService.SendAsync(
            equipment.CreatedBy.Id,
            $"审核驳回：{equipment.EquipmentNo}",
            $"您提交的设备【{equipment.EquipmentNo} {equipment.Name}】审核被驳回。原因：{remark}",
            $"/Equipment/Details/{equipmentId}");

        await WriteOperationLogAsync(auditorId, "AuditReject", equipmentId.ToString(),
            remark);

        return (true, null);
    }
}
