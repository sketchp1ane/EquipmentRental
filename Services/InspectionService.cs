using EquipmentRental.Constants;
using EquipmentRental.Data;
using EquipmentRental.Models;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Services;

public class InspectionService(
    AppDbContext db,
    FileService fileService,
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
            EntityType = "InspectionRecord",
            EntityId   = entityId,
            Detail     = detail,
            OccurredAt = DateTime.UtcNow,
            ClientIp   = ClientIp
        });
        await db.SaveChangesAsync();
    }

    // ── 获取创建表单数据 ──────────────────────────────────────────────────────

    public async Task<CreateInspectionViewModel?> GetCreateFormAsync(int orderId)
    {
        var order = await db.DispatchOrders
            .Include(o => o.Request)
            .Include(o => o.Equipment)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.Status != DispatchOrderStatus.InProgress)
            return null;

        return new CreateInspectionViewModel
        {
            OrderId      = orderId,
            ProjectName  = order.Request.ProjectName,
            EquipmentNo  = order.Equipment.EquipmentNo,
            InspectionDate = DateOnly.FromDateTime(DateTime.Today),
            Items        = InspectionChecklist.Standard
                .OrderBy(i => i.Order)
                .Select(i => new InspectionItemInputViewModel
                {
                    ItemKey  = i.Key,
                    ItemName = i.Name,
                    Order    = i.Order,
                    Status   = InspectionItemStatus.Normal
                })
                .ToList()
        };
    }

    // ── 创建巡检记录 ──────────────────────────────────────────────────────────

    public async Task<(bool Success, string? Error, int Id)> CreateInspectionAsync(
        CreateInspectionViewModel vm, string inspectorId)
    {
        if (vm.Images?.Count > 5)
            return (false, "巡检照片最多 5 张", 0);

        var order = await db.DispatchOrders
            .Include(o => o.Equipment)
            .FirstOrDefaultAsync(o => o.Id == vm.OrderId);

        if (order == null || order.Status != DispatchOrderStatus.InProgress)
            return (false, "调度单不存在或状态不符", 0);

        var record = new InspectionRecord
        {
            EquipmentId    = order.EquipmentId,
            OrderId        = vm.OrderId,
            InspectorId    = inspectorId,
            InspectionDate = vm.InspectionDate,
            OverallStatus  = vm.OverallStatus,
            Remark         = vm.Remark,
            CreatedAt      = DateTime.UtcNow
        };
        db.InspectionRecords.Add(record);
        await db.SaveChangesAsync();  // get Id

        var validKeys = InspectionChecklist.Standard.Select(i => i.Key).ToHashSet();
        foreach (var item in vm.Items ?? [])
        {
            if (!validKeys.Contains(item.ItemKey)) continue;
            db.InspectionItemResults.Add(new InspectionItemResult
            {
                InspectionId = record.Id,
                ItemKey      = item.ItemKey,
                Status       = item.Status,
                Remark       = string.IsNullOrWhiteSpace(item.Remark) ? null : item.Remark.Trim()
            });
        }

        if (vm.Images != null)
        {
            foreach (var file in vm.Images)
            {
                var path = await fileService.SaveFileAsync(file, "Inspection");
                db.InspectionImages.Add(new InspectionImage
                {
                    InspectionId = record.Id,
                    FilePath     = path,
                    UploadedAt   = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }

        await WriteOperationLogAsync(inspectorId, "Create", record.Id.ToString(),
            $"{{\"OrderId\":{vm.OrderId},\"OverallStatus\":\"{vm.OverallStatus}\"}}");

        return (true, null, record.Id);
    }

    // ── 详情 ──────────────────────────────────────────────────────────────────

    public async Task<InspectionDetailViewModel?> GetDetailAsync(int id)
    {
        var record = await db.InspectionRecords
            .Include(ir => ir.Order)
                .ThenInclude(o => o.Request)
            .Include(ir => ir.Order)
                .ThenInclude(o => o.Equipment)
            .Include(ir => ir.Inspector)
            .Include(ir => ir.Images)
            .Include(ir => ir.ItemResults)
            .FirstOrDefaultAsync(ir => ir.Id == id);

        if (record == null) return null;

        var resultByKey = record.ItemResults.ToDictionary(r => r.ItemKey);
        var itemResults = InspectionChecklist.Standard
            .OrderBy(i => i.Order)
            .Select(i => new InspectionItemResultViewModel
            {
                ItemKey  = i.Key,
                ItemName = i.Name,
                Order    = i.Order,
                Status   = resultByKey.TryGetValue(i.Key, out var r) ? r.Status : InspectionItemStatus.NotApplicable,
                Remark   = resultByKey.TryGetValue(i.Key, out var r2) ? r2.Remark : null
            })
            .Where(x => resultByKey.ContainsKey(x.ItemKey))
            .ToList();

        return new InspectionDetailViewModel
        {
            Id             = record.Id,
            OrderId        = record.OrderId,
            ProjectName    = record.Order.Request.ProjectName,
            EquipmentNo    = record.Order.Equipment.EquipmentNo,
            EquipmentName  = record.Order.Equipment.Name,
            InspectionDate = record.InspectionDate,
            OverallStatus  = record.OverallStatus,
            Remark         = record.Remark,
            InspectorName  = record.Inspector.RealName,
            CreatedAt      = record.CreatedAt,
            Images         = record.Images.Select(i => new InspectionImageViewModel
            {
                Id         = i.Id,
                FilePath   = i.FilePath,
                UploadedAt = i.UploadedAt
            }).ToList(),
            ItemResults    = itemResults
        };
    }

    // ── 列表 ──────────────────────────────────────────────────────────────────

    public async Task<InspectionListViewModel> GetListAsync(int page = 1, int pageSize = 15)
    {
        var query = db.InspectionRecords
            .Include(ir => ir.Order)
                .ThenInclude(o => o.Request)
            .Include(ir => ir.Order)
                .ThenInclude(o => o.Equipment)
            .Include(ir => ir.Inspector)
            .OrderByDescending(ir => ir.CreatedAt);

        int total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ir => new InspectionListItemViewModel
            {
                Id             = ir.Id,
                OrderId        = ir.OrderId,
                ProjectName    = ir.Order.Request.ProjectName,
                EquipmentNo    = ir.Order.Equipment.EquipmentNo,
                InspectionDate = ir.InspectionDate,
                OverallStatus  = ir.OverallStatus,
                InspectorName  = ir.Inspector.RealName
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

        return new InspectionListViewModel
        {
            Items           = items,
            TotalCount      = total,
            Page            = page,
            PageSize        = pageSize,
            AvailableOrders = availableOrders
        };
    }
}
