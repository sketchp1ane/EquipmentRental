using EquipmentRental.Data;
using EquipmentRental.Models;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Services;

public class QualificationService(
    AppDbContext db,
    FileService fileService,
    NotificationService notificationService,
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor)
{
    private string? ClientIp =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private static string TypeToString(QualificationType t) => t switch
    {
        QualificationType.ProductCertificate      => "产品合格证",
        QualificationType.FactoryInspectionReport => "出厂检验报告",
        QualificationType.SpecialEquipmentCert    => "特种设备使用登记证",
        QualificationType.AnnualInspectionReport  => "年度检验报告",
        QualificationType.InsuranceCertificate    => "保险凭证",
        QualificationType.InstallationQualification => "安装资质证明",
        _ => t.ToString()
    };

    private static QualificationListItemViewModel ToListItem(Qualification q)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysLeft = q.ValidTo.DayNumber - today.DayNumber;
        return new QualificationListItemViewModel
        {
            Id            = q.Id,
            Type          = q.Type,
            TypeName      = TypeToString(q.Type),
            CertNo        = q.CertNo,
            IssuedBy      = q.IssuedBy,
            ValidFrom     = q.ValidFrom,
            ValidTo       = q.ValidTo,
            IsExpired     = daysLeft < 0,
            IsExpiringSoon = daysLeft >= 0 && daysLeft <= 30,
            FilePath      = q.FilePath,
            UpdatedAt     = q.UpdatedAt
        };
    }

    private async Task WriteOperationLogAsync(string actorId, string action, string entityId, string? detail = null)
    {
        db.OperationLogs.Add(new OperationLog
        {
            UserId     = actorId,
            Action     = action,
            EntityType = "Qualification",
            EntityId   = entityId,
            Detail     = detail,
            OccurredAt = DateTime.UtcNow,
            ClientIp   = ClientIp
        });
        await db.SaveChangesAsync();
    }

    // Send expiry notifications to all Admin + DeviceAdmin users
    private async Task SendExpiryNotificationsAsync(int equipmentId, string equipmentNo, string certTypeName, DateOnly validTo)
    {
        var adminUsers = await userManager.GetUsersInRoleAsync(Constants.Roles.Admin);
        var deviceAdmins = await userManager.GetUsersInRoleAsync(Constants.Roles.DeviceAdmin);
        var recipients = adminUsers.Concat(deviceAdmins)
            .Select(u => u.Id)
            .Distinct();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysLeft = validTo.DayNumber - today.DayNumber;
        var statusText = daysLeft < 0
            ? $"已过期 {-daysLeft} 天"
            : $"还剩 {daysLeft} 天到期";

        foreach (var userId in recipients)
        {
            await notificationService.SendAsync(
                userId,
                $"证件到期提醒：{equipmentNo}",
                $"设备【{equipmentNo}】的【{certTypeName}】{statusText}，请及时处理。",
                $"/Qualification/Index/{equipmentId}");
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<QualificationIndexViewModel?> GetIndexAsync(int equipmentId)
    {
        var equipment = await db.Equipments
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == equipmentId);

        if (equipment == null) return null;

        var qualifications = await db.Qualifications
            .Where(q => q.EquipmentId == equipmentId)
            .OrderBy(q => q.Type)
            .ToListAsync();

        return new QualificationIndexViewModel
        {
            EquipmentId     = equipment.Id,
            EquipmentNo     = equipment.EquipmentNo,
            EquipmentName   = equipment.Name,
            EquipmentStatus = equipment.Status,
            Items           = qualifications.Select(ToListItem).ToList()
        };
    }

    public async Task<QualificationFormViewModel> GetCreateViewModelAsync(int equipmentId)
    {
        var equipment = await db.Equipments.FindAsync(equipmentId);
        return new QualificationFormViewModel
        {
            EquipmentId   = equipmentId,
            EquipmentNo   = equipment?.EquipmentNo ?? string.Empty,
            EquipmentName = equipment?.Name ?? string.Empty,
            ValidFrom     = DateOnly.FromDateTime(DateTime.Today),
            ValidTo       = DateOnly.FromDateTime(DateTime.Today.AddYears(1))
        };
    }

    public async Task<QualificationFormViewModel?> GetForEditAsync(int id)
    {
        var q = await db.Qualifications
            .Include(q => q.Equipment)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (q == null) return null;

        return new QualificationFormViewModel
        {
            Id                = q.Id,
            EquipmentId       = q.EquipmentId,
            EquipmentNo       = q.Equipment.EquipmentNo,
            EquipmentName     = q.Equipment.Name,
            Type              = q.Type,
            CertNo            = q.CertNo,
            IssuedBy          = q.IssuedBy,
            ValidFrom         = q.ValidFrom,
            ValidTo           = q.ValidTo,
            ExistingFilePath  = q.FilePath
        };
    }

    public async Task<(bool Success, string? Error)> CreateAsync(QualificationFormViewModel vm, string actorId)
    {
        var equipment = await db.Equipments.FindAsync(vm.EquipmentId);
        if (equipment == null) return (false, "设备不存在");

        string? filePath = null;
        if (vm.CertFile != null)
        {
            try { filePath = await fileService.SaveFileAsync(vm.CertFile, "Qualifications"); }
            catch (InvalidOperationException ex) { return (false, ex.Message); }
        }

        var q = new Qualification
        {
            EquipmentId = vm.EquipmentId,
            Type        = vm.Type,
            CertNo      = vm.CertNo,
            IssuedBy    = vm.IssuedBy,
            ValidFrom   = vm.ValidFrom,
            ValidTo     = vm.ValidTo,
            FilePath    = filePath,
            UpdatedAt   = DateTime.UtcNow
        };
        db.Qualifications.Add(q);
        await db.SaveChangesAsync();

        await WriteOperationLogAsync(actorId, "CreateQualification", q.Id.ToString(),
            $"设备 {equipment.EquipmentNo}，类型 {TypeToString(vm.Type)}");

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (vm.ValidTo <= today.AddDays(30))
            await SendExpiryNotificationsAsync(vm.EquipmentId, equipment.EquipmentNo, TypeToString(vm.Type), vm.ValidTo);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(QualificationFormViewModel vm, string actorId)
    {
        var q = await db.Qualifications
            .Include(q => q.Equipment)
            .FirstOrDefaultAsync(q => q.Id == vm.Id);

        if (q == null) return (false, "证件记录不存在");

        if (vm.RemoveFile && q.FilePath != null)
        {
            DeletePhysicalFile(q.FilePath);
            q.FilePath = null;
        }

        if (vm.CertFile != null)
        {
            if (q.FilePath != null) DeletePhysicalFile(q.FilePath);
            try { q.FilePath = await fileService.SaveFileAsync(vm.CertFile, "Qualifications"); }
            catch (InvalidOperationException ex) { return (false, ex.Message); }
        }

        q.Type      = vm.Type;
        q.CertNo    = vm.CertNo;
        q.IssuedBy  = vm.IssuedBy;
        q.ValidFrom = vm.ValidFrom;
        q.ValidTo   = vm.ValidTo;
        q.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await WriteOperationLogAsync(actorId, "UpdateQualification", q.Id.ToString(),
            $"设备 {q.Equipment.EquipmentNo}，类型 {TypeToString(vm.Type)}");

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (vm.ValidTo <= today.AddDays(30))
            await SendExpiryNotificationsAsync(q.EquipmentId, q.Equipment.EquipmentNo, TypeToString(vm.Type), vm.ValidTo);

        return (true, null);
    }

    public async Task<(bool Success, int EquipmentId, string? Error)> DeleteAsync(int id, string actorId)
    {
        var q = await db.Qualifications.FindAsync(id);
        if (q == null) return (false, 0, "证件记录不存在");

        var equipmentId = q.EquipmentId;
        if (q.FilePath != null) DeletePhysicalFile(q.FilePath);

        db.Qualifications.Remove(q);
        await db.SaveChangesAsync();

        await WriteOperationLogAsync(actorId, "DeleteQualification", id.ToString());

        return (true, equipmentId, null);
    }

    public async Task<IList<ExpiringCertViewModel>> GetExpiringAsync(int days = 30)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var threshold = today.AddDays(days);

        var items = await db.Qualifications
            .Include(q => q.Equipment)
            .Where(q => q.ValidTo <= threshold)
            .OrderBy(q => q.ValidTo)
            .Select(q => new
            {
                q.EquipmentId,
                q.Equipment.EquipmentNo,
                q.Equipment.Name,
                q.Type,
                q.ValidTo
            })
            .ToListAsync();

        return items.Select(x =>
        {
            var daysLeft = x.ValidTo.DayNumber - today.DayNumber;
            return new ExpiringCertViewModel
            {
                EquipmentId  = x.EquipmentId,
                EquipmentNo  = x.EquipmentNo,
                EquipmentName = x.Name,
                CertTypeName = TypeToString(x.Type),
                ValidTo      = x.ValidTo,
                DaysLeft     = daysLeft,
                IsExpired    = daysLeft < 0
            };
        }).ToList();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public IList<QualificationListItemViewModel> MapToListItems(IList<Qualification> qualifications)
        => qualifications.Select(ToListItem).ToList();

    private static void DeletePhysicalFile(string relativePath)
    {
        try
        {
            // relativePath is like "Uploads/Qualifications/xxx.pdf"
            // We need to resolve from content root — use a simple absolute approach
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
            if (File.Exists(basePath)) File.Delete(basePath);
        }
        catch { /* ignore file deletion errors */ }
    }
}
