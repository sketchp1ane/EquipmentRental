using EquipmentRental.Constants;
using EquipmentRental.Data;
using EquipmentRental.Models;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Services;

public class FaultService(
    AppDbContext db,
    FileService fileService,
    NotificationService notificationService,
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor)
{
    private string? ClientIp =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private async Task WriteOperationLogAsync(
        string actorId, string action, string entityId, string? detail = null)
    {
        db.OperationLogs.Add(new OperationLog
        {
            UserId     = actorId,
            Action     = action,
            EntityType = "FaultReport",
            EntityId   = entityId,
            Detail     = detail,
            OccurredAt = DateTime.UtcNow,
            ClientIp   = ClientIp
        });
        await db.SaveChangesAsync();
    }

    // ── 获取创建表单数据 ──────────────────────────────────────────────────────

    public async Task<CreateFaultViewModel?> GetCreateFormAsync(int orderId)
    {
        var order = await db.DispatchOrders
            .Include(o => o.Request)
            .Include(o => o.Equipment)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.Status != DispatchOrderStatus.InProgress)
            return null;

        return new CreateFaultViewModel
        {
            OrderId     = orderId,
            ProjectName = order.Request.ProjectName,
            EquipmentNo = order.Equipment.EquipmentNo
        };
    }

    // ── 故障上报 ──────────────────────────────────────────────────────────────

    public async Task<(bool Success, string? Error, int Id)> CreateFaultAsync(
        CreateFaultViewModel vm, string reporterId)
    {
        if (vm.Images?.Count > 10)
            return (false, "故障照片最多 10 张", 0);

        var order = await db.DispatchOrders
            .Include(o => o.Equipment)
            .Include(o => o.Request)
            .FirstOrDefaultAsync(o => o.Id == vm.OrderId);

        if (order == null || order.Status != DispatchOrderStatus.InProgress)
            return (false, "调度单不存在或状态不符", 0);

        var report = new FaultReport
        {
            EquipmentId = order.EquipmentId,
            OrderId     = vm.OrderId,
            ReporterId  = reporterId,
            Description = vm.Description,
            Severity    = vm.Severity,
            ReportedAt  = DateTime.UtcNow,
            Status      = FaultStatus.Pending
        };
        db.FaultReports.Add(report);
        await db.SaveChangesAsync();  // get Id

        if (vm.Images != null)
        {
            foreach (var file in vm.Images)
            {
                var path = await fileService.SaveFileAsync(file, "Fault");
                db.FaultImages.Add(new FaultImage
                {
                    FaultReportId = report.Id,
                    FilePath      = path,
                    UploadedAt    = DateTime.UtcNow
                });
            }
        }

        // 设备状态 → 维修中
        order.Equipment.Status = EquipmentStatus.Maintenance;

        await db.SaveChangesAsync();

        await WriteOperationLogAsync(reporterId, "Create", report.Id.ToString(),
            $"{{\"OrderId\":{vm.OrderId},\"Severity\":\"{vm.Severity}\"}}");

        // 通知所有设备管理员
        var admins = await userManager.GetUsersInRoleAsync(Roles.DeviceAdmin);
        foreach (var admin in admins)
        {
            await notificationService.SendAsync(
                admin.Id,
                "新故障上报",
                $"设备 {order.Equipment.EquipmentNo}（{order.Request.ProjectName}）上报故障，请前往处理。",
                $"/Fault/Details/{report.Id}");
        }

        return (true, null, report.Id);
    }

    // ── 受理工单 ──────────────────────────────────────────────────────────────

    public async Task<(bool Success, string? Error)> AcceptFaultAsync(int id, string operatorId)
    {
        var report = await db.FaultReports.FindAsync(id);

        if (report == null)
            return (false, "故障工单不存在");

        if (report.Status != FaultStatus.Pending)
            return (false, "故障工单当前状态不可受理");

        report.Status = FaultStatus.InProgress;
        await db.SaveChangesAsync();

        await WriteOperationLogAsync(operatorId, "Accept", id.ToString());

        return (true, null);
    }

    // ── 关闭工单 + 恢复设备 ───────────────────────────────────────────────────

    private static string GetEquipmentStatusText(EquipmentStatus status) => status switch
    {
        EquipmentStatus.Idle        => "空闲",
        EquipmentStatus.InUse       => "使用中",
        EquipmentStatus.Maintenance => "维修中",
        EquipmentStatus.Scrapped    => "已报废",
        _                           => "未知"
    };

    public async Task<(bool Success, string? Error, string RestoredStatusText)> CloseFaultAsync(
        CloseFaultViewModel vm, string operatorId)
    {
        var report = await db.FaultReports
            .Include(fr => fr.Equipment)
            .Include(fr => fr.Order)
            .Include(fr => fr.Reporter)
            .FirstOrDefaultAsync(fr => fr.Id == vm.Id);

        if (report == null)
            return (false, "故障工单不存在", string.Empty);

        if (report.Status != FaultStatus.InProgress)
            return (false, "故障工单当前状态不可关闭", string.Empty);

        report.Status     = FaultStatus.Closed;
        report.Resolution = vm.Resolution;
        report.RepairCost = vm.RepairCost;
        report.ClosedById = operatorId;
        report.ClosedAt   = DateTime.UtcNow;

        // 活跃调度单上的故障关闭后，设备应恢复为使用中；否则恢复为空闲。
        report.Equipment.Status = report.Order.Status == DispatchOrderStatus.InProgress
            ? EquipmentStatus.InUse
            : EquipmentStatus.Idle;

        var restoredStatusText = GetEquipmentStatusText(report.Equipment.Status);

        await db.SaveChangesAsync();

        await WriteOperationLogAsync(operatorId, "Close", vm.Id.ToString(),
            $"{{\"Resolution\":\"{vm.Resolution}\",\"RepairCost\":{vm.RepairCost}}}");

        // 通知上报人
        await notificationService.SendAsync(
            report.ReporterId,
            "故障工单已关闭",
            $"您上报的故障（工单 #{report.Id}）已处理完成，设备已恢复为{restoredStatusText}状态。",
            $"/Fault/Details/{report.Id}");

        return (true, null, restoredStatusText);
    }

    // ── 详情 ──────────────────────────────────────────────────────────────────

    public async Task<FaultDetailViewModel?> GetDetailAsync(int id)
    {
        var report = await db.FaultReports
            .Include(fr => fr.Order)
                .ThenInclude(o => o.Request)
            .Include(fr => fr.Order)
                .ThenInclude(o => o.Equipment)
            .Include(fr => fr.Reporter)
            .Include(fr => fr.ClosedBy)
            .Include(fr => fr.Images)
            .FirstOrDefaultAsync(fr => fr.Id == id);

        if (report == null) return null;

        return new FaultDetailViewModel
        {
            Id            = report.Id,
            OrderId       = report.OrderId,
            ProjectName   = report.Order.Request.ProjectName,
            EquipmentNo   = report.Order.Equipment.EquipmentNo,
            EquipmentName = report.Order.Equipment.Name,
            Description   = report.Description,
            Severity      = report.Severity,
            ReporterName  = report.Reporter.RealName,
            ReportedAt    = report.ReportedAt,
            Status        = report.Status,
            Resolution    = report.Resolution,
            RepairCost    = report.RepairCost,
            ClosedByName  = report.ClosedBy?.RealName,
            ClosedAt      = report.ClosedAt,
            RestoredEquipmentStatusText = GetEquipmentStatusText(report.Order.Equipment.Status),
            Images        = report.Images.Select(i => new FaultImageViewModel
            {
                Id         = i.Id,
                FilePath   = i.FilePath,
                UploadedAt = i.UploadedAt
            }).ToList(),
            CanAccept = report.Status == FaultStatus.Pending,
            CanClose  = report.Status == FaultStatus.InProgress
        };
    }

    // ── 列表 ──────────────────────────────────────────────────────────────────

    public async Task<FaultListViewModel> GetListAsync(int page = 1, int pageSize = 15)
    {
        var query = db.FaultReports
            .Include(fr => fr.Order)
                .ThenInclude(o => o.Request)
            .Include(fr => fr.Order)
                .ThenInclude(o => o.Equipment)
            .Include(fr => fr.Reporter)
            .OrderByDescending(fr => fr.ReportedAt);

        int total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(fr => new FaultListItemViewModel
            {
                Id           = fr.Id,
                OrderId      = fr.OrderId,
                ProjectName  = fr.Order.Request.ProjectName,
                EquipmentNo  = fr.Order.Equipment.EquipmentNo,
                Severity     = fr.Severity,
                ReporterName = fr.Reporter.RealName,
                ReportedAt   = fr.ReportedAt,
                Status       = fr.Status
            })
            .ToListAsync();

        var availableOrders = await db.DispatchOrders
            .Where(o => o.Status == DispatchOrderStatus.InProgress)
            .Include(o => o.Request)
            .Include(o => o.Equipment)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new InProgressOrderOptionViewModel
            {
                OrderId       = o.Id,
                ProjectName   = o.Request.ProjectName,
                EquipmentNo   = o.Equipment.EquipmentNo,
                EquipmentName = o.Equipment.Name
            })
            .ToListAsync();

        return new FaultListViewModel
        {
            Items           = items,
            TotalCount      = total,
            Page            = page,
            PageSize        = pageSize,
            AvailableOrders = availableOrders
        };
    }
}
