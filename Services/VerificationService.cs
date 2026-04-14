using EquipmentRental.Data;
using EquipmentRental.Models;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Services;

public class VerificationService(
    AppDbContext db,
    NotificationService notificationService,
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
            EntityType = "EntryVerification",
            EntityId   = entityId,
            Detail     = detail,
            OccurredAt = DateTime.UtcNow,
            ClientIp   = ClientIp
        });
        await db.SaveChangesAsync();
    }

    // ── 4.5.1 获取核验表单（含订单预览） ──────────────────────────────────────

    public async Task<VerifyFormViewModel> GetVerifyFormAsync(string? code)
    {
        var vm = new VerifyFormViewModel { VerifyCode = code };

        if (string.IsNullOrWhiteSpace(code))
            return vm;

        var order = await db.DispatchOrders
            .Include(o => o.Equipment)
            .Include(o => o.Request)
            .FirstOrDefaultAsync(o => o.VerifyCode == code);

        if (order == null)
            return vm;

        vm.HasOrderInfo   = true;
        vm.OrderId        = order.Id;
        vm.EquipmentNo    = order.Equipment.EquipmentNo;
        vm.EquipmentName  = order.Equipment.Name;
        vm.ProjectName    = order.Request.ProjectName;
        vm.ActualStart    = order.ActualStart;
        vm.ActualEnd      = order.ActualEnd;
        vm.OrderStatus    = order.Status;

        return vm;
    }

    // ── 4.5.2 执行核验 ────────────────────────────────────────────────────────

    public async Task<(bool Success, bool IsPass, string? FailReason, int? VerificationId)>
        PerformVerifyAsync(string verifyCode, string verifierId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        // 1. 找到调度单且状态为已签署
        var order = await db.DispatchOrders
            .Include(o => o.Equipment)
            .Include(o => o.Request)
            .Include(o => o.Dispatcher)
            .FirstOrDefaultAsync(o => o.VerifyCode == verifyCode);

        if (order == null || order.Status != DispatchOrderStatus.Signed)
            return await RecordFailAsync(order?.Id, verifierId,
                "核验码无效或调度单尚未签署");

        // 2. 核验码有效期（开始日期起 3 天内）
        if (today > order.ActualStart.AddDays(3))
            return await RecordFailAsync(order.Id, verifierId,
                "核验码已过期（有效期为租赁开始日期后 3 天）");

        // 3. 未曾通过核验
        bool alreadyPassed = await db.EntryVerifications
            .AnyAsync(ev => ev.OrderId == order.Id && ev.IsPass);
        if (alreadyPassed)
            return await RecordFailAsync(order.Id, verifierId,
                "该调度单已完成进场核验");

        // 4. 设备状态为出租中
        if (order.Equipment.Status != EquipmentStatus.InUse)
            return await RecordFailAsync(order.Id, verifierId,
                "设备当前状态异常，无法核验（设备需处于出租中状态）");

        // 5. 所有证件在有效期内
        var expiredCerts = await db.Qualifications
            .Where(q => q.EquipmentId == order.EquipmentId && q.ValidTo < today)
            .ToListAsync();

        if (expiredCerts.Count > 0)
        {
            var names = expiredCerts.Select(q => q.Type.ToString()).Distinct();
            return await RecordFailAsync(order.Id, verifierId,
                $"以下设备证件已过期：{string.Join("、", names)}");
        }

        // 全部通过 ─ 记录核验结果并推进状态
        var verification = new EntryVerification
        {
            OrderId    = order.Id,
            VerifierId = verifierId,
            VerifiedAt = DateTime.UtcNow,
            IsPass     = true
        };
        db.EntryVerifications.Add(verification);
        order.Status = DispatchOrderStatus.InProgress;
        await db.SaveChangesAsync();

        await WriteOperationLogAsync(verifierId, "Verify", verification.Id.ToString(),
            $"{{\"OrderId\":{order.Id},\"IsPass\":true}}");

        // 通知调度员
        await notificationService.SendAsync(
            order.DispatcherId,
            "进场核验通过",
            $"调度单 #{order.Id}（{order.Request.ProjectName}）已完成进场核验。",
            $"/Verification/Details/{verification.Id}");

        return (true, true, null, verification.Id);
    }

    // ── 核验失败记录 ──────────────────────────────────────────────────────────

    private async Task<(bool Success, bool IsPass, string? FailReason, int? VerificationId)>
        RecordFailAsync(int? orderId, string verifierId, string reason)
    {
        if (orderId.HasValue)
        {
            var verification = new EntryVerification
            {
                OrderId    = orderId.Value,
                VerifierId = verifierId,
                VerifiedAt = DateTime.UtcNow,
                IsPass     = false,
                FailReason = reason
            };
            db.EntryVerifications.Add(verification);
            await db.SaveChangesAsync();

            await WriteOperationLogAsync(verifierId, "VerifyFail", verification.Id.ToString(),
                $"{{\"OrderId\":{orderId.Value},\"IsPass\":false,\"Reason\":\"{reason}\"}}");

            return (true, false, reason, verification.Id);
        }

        // 无法找到调度单时，不写记录，直接返回错误
        return (false, false, reason, null);
    }

    // ── 核验详情 ──────────────────────────────────────────────────────────────

    public async Task<VerificationDetailViewModel?> GetVerificationDetailAsync(int id)
    {
        var ev = await db.EntryVerifications
            .Include(v => v.Order)
                .ThenInclude(o => o.Equipment)
            .Include(v => v.Order)
                .ThenInclude(o => o.Request)
            .Include(v => v.Verifier)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (ev == null) return null;

        return new VerificationDetailViewModel
        {
            Id           = ev.Id,
            OrderId      = ev.OrderId,
            ProjectName  = ev.Order.Request.ProjectName,
            EquipmentNo  = ev.Order.Equipment.EquipmentNo,
            EquipmentName = ev.Order.Equipment.Name,
            ActualStart  = ev.Order.ActualStart,
            ActualEnd    = ev.Order.ActualEnd,
            VerifierName = ev.Verifier.RealName,
            VerifiedAt   = ev.VerifiedAt,
            IsPass       = ev.IsPass,
            FailReason   = ev.FailReason
        };
    }

    // ── 核验列表 ──────────────────────────────────────────────────────────────

    public async Task<VerificationListViewModel> GetVerificationListAsync(int page = 1, int pageSize = 15)
    {
        var query = db.EntryVerifications
            .Include(v => v.Order)
                .ThenInclude(o => o.Equipment)
            .Include(v => v.Order)
                .ThenInclude(o => o.Request)
            .Include(v => v.Verifier)
            .OrderByDescending(v => v.VerifiedAt);

        int total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VerificationListItemViewModel
            {
                Id           = v.Id,
                OrderId      = v.OrderId,
                EquipmentNo  = v.Order.Equipment.EquipmentNo,
                ProjectName  = v.Order.Request.ProjectName,
                VerifierName = v.Verifier.RealName,
                VerifiedAt   = v.VerifiedAt,
                IsPass       = v.IsPass
            })
            .ToListAsync();

        return new VerificationListViewModel
        {
            Items     = items,
            TotalCount = total,
            Page      = page,
            PageSize  = pageSize
        };
    }
}
