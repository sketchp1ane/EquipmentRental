using EquipmentRental.Data;
using EquipmentRental.Models;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Services;

public class ReturnService(
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
            EntityType = "ReturnApplication",
            EntityId   = entityId,
            Detail     = detail,
            OccurredAt = DateTime.UtcNow,
            ClientIp   = ClientIp
        });
        await db.SaveChangesAsync();
    }

    // ── 退场申请表单数据 ──────────────────────────────────────────────────────

    public async Task<ApplyReturnViewModel?> GetApplyFormAsync(int orderId)
    {
        var order = await db.DispatchOrders
            .Include(o => o.Request)
            .Include(o => o.Equipment)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null || order.Status != DispatchOrderStatus.InProgress)
            return null;

        bool alreadyApplied = await db.ReturnApplications.AnyAsync(r => r.OrderId == orderId);
        if (alreadyApplied)
            return null;

        return new ApplyReturnViewModel
        {
            OrderId     = orderId,
            ProjectName = order.Request.ProjectName,
            EquipmentNo = order.Equipment.EquipmentNo
        };
    }

    // ── 提交退场申请 ──────────────────────────────────────────────────────────

    public async Task<(bool Success, string? Error, int Id)> CreateApplicationAsync(
        ApplyReturnViewModel vm, string applicantId)
    {
        var order = await db.DispatchOrders
            .FirstOrDefaultAsync(o => o.Id == vm.OrderId);

        if (order == null || order.Status != DispatchOrderStatus.InProgress)
            return (false, "调度单不存在或状态不符，无法提交退场申请", 0);

        bool alreadyApplied = await db.ReturnApplications.AnyAsync(r => r.OrderId == vm.OrderId);
        if (alreadyApplied)
            return (false, "该调度单已存在退场申请，不可重复提交", 0);

        var application = new ReturnApplication
        {
            OrderId          = vm.OrderId,
            ApplicantId      = applicantId,
            ActualReturnDate = vm.ActualReturnDate,
            ConditionDesc    = vm.ConditionDesc,
            Status           = ReturnApplicationStatus.PendingEvaluation,
            CreatedAt        = DateTime.UtcNow
        };
        db.ReturnApplications.Add(application);
        await db.SaveChangesAsync();

        await WriteOperationLogAsync(applicantId, "Create", application.Id.ToString(),
            $"{{\"OrderId\":{vm.OrderId},\"ActualReturnDate\":\"{vm.ActualReturnDate}\"}}");

        return (true, null, application.Id);
    }

    // ── 退场申请详情 ──────────────────────────────────────────────────────────

    public async Task<ReturnDetailViewModel?> GetDetailAsync(int id)
    {
        var app = await db.ReturnApplications
            .Include(r => r.Order)
                .ThenInclude(o => o.Request)
            .Include(r => r.Order)
                .ThenInclude(o => o.Equipment)
            .Include(r => r.Applicant)
            .Include(r => r.Evaluation)
                .ThenInclude(e => e!.Evaluator)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (app == null) return null;

        ReturnEvaluationDetailViewModel? evalVm = null;
        if (app.Evaluation != null)
        {
            var e = app.Evaluation;
            evalVm = new ReturnEvaluationDetailViewModel
            {
                Id                 = e.Id,
                EvaluatorName      = e.Evaluator.RealName,
                AppearanceScore    = e.AppearanceScore,
                FunctionScore      = e.FunctionScore,
                DamageDesc         = e.DamageDesc,
                Deduction          = e.Deduction,
                RefundAmount       = e.RefundAmount,
                Remark             = e.Remark,
                EvaluatedAt        = e.EvaluatedAt,
                NewEquipmentStatus = app.Order.Equipment.Status switch
                {
                    EquipmentStatus.Idle        => "空闲",
                    EquipmentStatus.Maintenance => "送修",
                    EquipmentStatus.Scrapped    => "已报废",
                    _                           => app.Order.Equipment.Status.ToString()
                }
            };
        }

        return new ReturnDetailViewModel
        {
            Id               = app.Id,
            OrderId          = app.OrderId,
            ProjectName      = app.Order.Request.ProjectName,
            EquipmentNo      = app.Order.Equipment.EquipmentNo,
            EquipmentName    = app.Order.Equipment.Name,
            ApplicantName    = app.Applicant.RealName,
            ActualReturnDate = app.ActualReturnDate,
            ConditionDesc    = app.ConditionDesc,
            Status           = app.Status,
            CreatedAt        = app.CreatedAt,
            Deposit          = app.Order.Deposit,
            Evaluation       = evalVm,
            CanEvaluate      = app.Evaluation == null && app.Status == ReturnApplicationStatus.PendingEvaluation
        };
    }

    // ── 评价表单数据 ──────────────────────────────────────────────────────────

    public async Task<EvaluateReturnViewModel?> GetEvaluateFormAsync(int returnAppId)
    {
        var app = await db.ReturnApplications
            .Include(r => r.Order)
                .ThenInclude(o => o.Request)
            .Include(r => r.Order)
                .ThenInclude(o => o.Equipment)
            .FirstOrDefaultAsync(r => r.Id == returnAppId);

        if (app == null || app.Status != ReturnApplicationStatus.PendingEvaluation)
            return null;

        if (app.Evaluation != null)
            return null;

        return new EvaluateReturnViewModel
        {
            ReturnAppId = returnAppId,
            ProjectName = app.Order.Request.ProjectName,
            EquipmentNo = app.Order.Equipment.EquipmentNo,
            Deposit     = app.Order.Deposit
        };
    }

    // ── 提交退场评价 ──────────────────────────────────────────────────────────

    public async Task<(bool Success, string? Error)> CreateEvaluationAsync(
        EvaluateReturnViewModel vm, string evaluatorId)
    {
        var app = await db.ReturnApplications
            .Include(r => r.Order)
                .ThenInclude(o => o.Equipment)
            .Include(r => r.Evaluation)
            .FirstOrDefaultAsync(r => r.Id == vm.ReturnAppId);

        if (app == null)
            return (false, "退场申请不存在");

        if (app.Status != ReturnApplicationStatus.PendingEvaluation)
            return (false, "退场申请当前状态不可填写评价");

        if (app.Evaluation != null)
            return (false, "该退场申请已填写过评价");

        if (vm.NewEquipmentStatus is not (EquipmentStatus.Idle or EquipmentStatus.Maintenance or EquipmentStatus.Scrapped))
            return (false, "请选择有效的退场后设备状态");

        // Server-side refund calculation — never trust client value
        decimal refundAmount = app.Order.Deposit - vm.Deduction;
        if (refundAmount < 0) refundAmount = 0;

        var evaluation = new ReturnEvaluation
        {
            ReturnAppId     = vm.ReturnAppId,
            EvaluatorId     = evaluatorId,
            AppearanceScore = vm.AppearanceScore,
            FunctionScore   = vm.FunctionScore,
            DamageDesc      = vm.DamageDesc,
            Deduction       = vm.Deduction,
            RefundAmount    = refundAmount,
            Remark          = vm.Remark,
            EvaluatedAt     = DateTime.UtcNow
        };
        db.ReturnEvaluations.Add(evaluation);

        // State transitions
        app.Status             = ReturnApplicationStatus.Complete;
        app.Order.Status       = DispatchOrderStatus.Complete;
        app.Order.Equipment.Status = vm.NewEquipmentStatus;

        await db.SaveChangesAsync();

        await WriteOperationLogAsync(evaluatorId, "Evaluate", app.Id.ToString(),
            $"{{\"ReturnAppId\":{vm.ReturnAppId},\"RefundAmount\":{refundAmount},\"NewStatus\":\"{vm.NewEquipmentStatus}\"}}");

        // 通知申请人（项目负责人）
        await notificationService.SendAsync(
            app.ApplicantId,
            "退场评价已完成",
            $"您提交的退场申请（调度单 #{app.OrderId}，设备 {app.Order.Equipment.EquipmentNo}）已完成评价，押金退还金额：¥{refundAmount:N2}。",
            $"/Return/Details/{app.Id}");

        return (true, null);
    }

    // ── 列表 ──────────────────────────────────────────────────────────────────

    public async Task<ReturnListViewModel> GetListAsync(
        string? restrictToUserId, int page = 1, int pageSize = 15)
    {
        var query = db.ReturnApplications
            .Include(r => r.Order)
                .ThenInclude(o => o.Request)
            .Include(r => r.Order)
                .ThenInclude(o => o.Equipment)
            .Include(r => r.Applicant)
            .AsQueryable();

        if (restrictToUserId != null)
        {
            query = query.Where(r => r.ApplicantId == restrictToUserId);
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        int total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReturnListItemViewModel
            {
                Id               = r.Id,
                OrderId          = r.OrderId,
                ProjectName      = r.Order.Request.ProjectName,
                EquipmentNo      = r.Order.Equipment.EquipmentNo,
                ApplicantName    = r.Applicant.RealName,
                ActualReturnDate = r.ActualReturnDate,
                Status           = r.Status,
                CreatedAt        = r.CreatedAt
            })
            .ToListAsync();

        var availableOrdersQuery = db.DispatchOrders
            .Where(o => o.Status == DispatchOrderStatus.InProgress
                     && !db.ReturnApplications.Any(r => r.OrderId == o.Id));

        if (restrictToUserId != null)
        {
            availableOrdersQuery = availableOrdersQuery
                .Where(o => o.Request.RequesterId == restrictToUserId);
        }

        var availableOrders = await availableOrdersQuery
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

        return new ReturnListViewModel
        {
            Items           = items,
            TotalCount      = total,
            Page            = page,
            PageSize        = pageSize,
            AvailableOrders = availableOrders
        };
    }
}
