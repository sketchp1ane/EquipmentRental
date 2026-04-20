using EquipmentRental.Constants;
using EquipmentRental.Data;
using EquipmentRental.Models;
using EquipmentRental.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EquipmentRental.Services;

public class DashboardService(AppDbContext db, UserManager<Models.Entities.ApplicationUser> userManager)
{
    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var statusCounts = await db.Equipments
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int GetCount(EquipmentStatus s) =>
            statusCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var activeOrders = await db.DispatchOrders.CountAsync(o => o.Status == DispatchOrderStatus.InProgress);

        var sixMonthsAgo = monthStart.AddMonths(-5);
        var recentOrders = await db.DispatchOrders
            .Where(o => o.CreatedAt >= sixMonthsAgo)
            .Select(o => new { o.CreatedAt, o.UnitPrice, StartDayNo = o.ActualStart.DayNumber, EndDayNo = o.ActualEnd.DayNumber })
            .ToListAsync();

        var trend = Enumerable.Range(0, 6)
            .Select(i => monthStart.AddMonths(-5 + i))
            .Select(m =>
            {
                var label   = m.ToString("yyyy-MM");
                var bucket  = recentOrders.Where(o => o.CreatedAt.Year == m.Year && o.CreatedAt.Month == m.Month).ToList();
                var revenue = bucket.Sum(o => o.UnitPrice * Math.Max(0, o.EndDayNo - o.StartDayNo));
                return new MonthlyCountDto(label, bucket.Count, revenue);
            })
            .ToList();

        return new DashboardStatsDto
        {
            EquipmentTotal         = statusCounts.Sum(x => x.Count),
            EquipmentPendingReview = GetCount(EquipmentStatus.PendingReview),
            EquipmentIdle          = GetCount(EquipmentStatus.Idle),
            EquipmentInUse         = GetCount(EquipmentStatus.InUse),
            EquipmentMaintenance   = GetCount(EquipmentStatus.Maintenance),
            EquipmentScrapped      = GetCount(EquipmentStatus.Scrapped),
            ActiveOrders           = activeOrders,
            MonthlyTrend           = trend
        };
    }

    public async Task<IList<PendingActionViewModel>> GetPendingActionsAsync(ClaimsPrincipal principal)
    {
        var userId = userManager.GetUserId(principal)!;
        var actions = new List<PendingActionViewModel>();

        bool isAdmin         = principal.IsInRole(Roles.Admin);
        bool isDispatcher    = principal.IsInRole(Roles.Dispatcher);
        bool isDeviceAdmin   = principal.IsInRole(Roles.DeviceAdmin);
        bool isProjectLead   = principal.IsInRole(Roles.ProjectLead);
        bool isSafetyOfficer = principal.IsInRole(Roles.SafetyOfficer);

        if (isDispatcher || isAdmin)
        {
            int pendingRequests = await db.DispatchRequests
                .CountAsync(r => r.Status == DispatchRequestStatus.Pending);
            if (pendingRequests > 0)
                actions.Add(new("待处理用车申请", pendingRequests, "/Dispatch", "danger"));

            int unsignedContracts = await db.Contracts
                .CountAsync(c => c.Status != ContractStatus.Signed
                              && c.Status != ContractStatus.Terminated);
            if (unsignedContracts > 0)
                actions.Add(new("待签署合同", unsignedContracts, "/Dispatch/Orders?status=Unsigned", "warning"));
        }

        if (isDeviceAdmin || isAdmin)
        {
            int pendingReviewEquipments = await db.Equipments
                .CountAsync(e => e.Status == EquipmentStatus.PendingReview);
            if (pendingReviewEquipments > 0)
                actions.Add(new("待审核设备", pendingReviewEquipments, "/Audit", "warning"));

            int pendingFaults = await db.FaultReports
                .CountAsync(f => f.Status == FaultStatus.Pending);
            if (pendingFaults > 0)
                actions.Add(new("待处理故障工单", pendingFaults, "/Fault", "danger"));

            int pendingEvaluations = await db.ReturnApplications
                .CountAsync(r => r.Status == ReturnApplicationStatus.PendingEvaluation);
            if (pendingEvaluations > 0)
                actions.Add(new("待评价退场申请", pendingEvaluations, "/Return", "info"));
        }

        if (isProjectLead || isAdmin)
        {
            int unverifiedOrders = await db.DispatchOrders
                .CountAsync(o => o.Status == DispatchOrderStatus.Signed
                              && o.EntryVerification == null
                              && (isAdmin || o.Request.RequesterId == userId));
            if (unverifiedOrders > 0)
                actions.Add(new("待进场核验", unverifiedOrders, "/Dispatch/Orders?status=Signed", "warning"));

            int draftBriefingsForLead = await db.SafetyBriefings
                .CountAsync(sb => sb.Status == SafetyBriefingStatus.Draft
                               && (isAdmin || sb.Order.Request.RequesterId == userId));
            if (draftBriefingsForLead > 0)
                actions.Add(new("待签署安全交底", draftBriefingsForLead, "/Safety/List", "info"));

            int canApplyReturn = await db.DispatchOrders
                .CountAsync(o => o.Status == DispatchOrderStatus.InProgress
                              && o.ReturnApplication == null
                              && (isAdmin || o.Request.RequesterId == userId));
            if (canApplyReturn > 0)
                actions.Add(new("可申请退场的调度单", canApplyReturn, "/Return", "secondary"));
        }

        if (isSafetyOfficer || isAdmin)
        {
            int myDraftBriefings = await db.SafetyBriefings
                .CountAsync(sb => sb.Status == SafetyBriefingStatus.Draft
                               && (isAdmin || sb.CreatorId == userId));
            if (myDraftBriefings > 0 && !actions.Any(a => a.Title == "待签署安全交底"))
                actions.Add(new("待签署安全交底", myDraftBriefings, "/Safety/List", "info"));

            int inProgressOrdersForInspection = await db.DispatchOrders
                .CountAsync(o => o.Status == DispatchOrderStatus.InProgress);
            if (inProgressOrdersForInspection > 0)
                actions.Add(new("进行中调度单（可巡检）", inProgressOrdersForInspection, "/Inspection", "success"));
        }

        return actions;
    }
}
