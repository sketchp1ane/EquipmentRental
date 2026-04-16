using EquipmentRental.Data;
using EquipmentRental.Models;
using EquipmentRental.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace EquipmentRental.Services;

public class ReportService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    // ── Dashboard ─────────────────────────────────────────────────────────

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var statusCounts = await _db.Equipments
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        int GetCount(EquipmentStatus s) =>
            statusCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var activeOrders    = await _db.DispatchOrders.CountAsync(o => o.Status == DispatchOrderStatus.InProgress);
        var thisMonthOrders = await _db.DispatchOrders.CountAsync(o => o.CreatedAt >= monthStart);
        var pendingFaults   = await _db.FaultReports.CountAsync(f => f.Status != FaultStatus.Closed);

        // Last 6 months trend (query raw then group in-memory)
        var sixMonthsAgo = monthStart.AddMonths(-5);
        var recentOrders = await _db.DispatchOrders
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
            EquipmentPendingReview = GetCount(EquipmentStatus.PendingReview),
            EquipmentIdle          = GetCount(EquipmentStatus.Idle),
            EquipmentInUse         = GetCount(EquipmentStatus.InUse),
            EquipmentMaintenance   = GetCount(EquipmentStatus.Maintenance),
            EquipmentScrapped      = GetCount(EquipmentStatus.Scrapped),
            ActiveOrders           = activeOrders,
            ThisMonthOrders        = thisMonthOrders,
            PendingFaults          = pendingFaults,
            MonthlyTrend           = trend
        };
    }

    // ── Rental Statistics ─────────────────────────────────────────────────

    public async Task<RentalStatsDto> GetRentalStatsAsync(DateOnly from, DateOnly to)
    {
        var orders = await _db.DispatchOrders
            .Include(o => o.Request)
            .Include(o => o.Equipment)
            .Where(o => o.ActualStart <= to && o.ActualEnd >= from)
            .OrderBy(o => o.ActualStart)
            .ToListAsync();

        var monthly = orders
            .GroupBy(o => o.ActualStart.ToString("yyyy-MM"))
            .OrderBy(g => g.Key)
            .Select(g => new MonthlyCountDto(
                g.Key,
                g.Count(),
                g.Sum(o => o.UnitPrice * Math.Max(0, o.ActualEnd.DayNumber - o.ActualStart.DayNumber))))
            .ToList();

        var details = orders.Select(o => new RentalDetailDto
        {
            ProjectName      = o.Request.ProjectName,
            EquipmentName    = o.Equipment.Name,
            ActualStart      = o.ActualStart,
            ActualEnd        = o.ActualEnd,
            EstimatedRevenue = o.UnitPrice * Math.Max(0, o.ActualEnd.DayNumber - o.ActualStart.DayNumber),
            Status           = OrderStatusToString(o.Status)
        }).ToList();

        return new RentalStatsDto
        {
            Total      = orders.Count,
            Completed  = orders.Count(o => o.Status == DispatchOrderStatus.Complete),
            InProgress = orders.Count(o => o.Status == DispatchOrderStatus.InProgress),
            Terminated = orders.Count(o => o.Status == DispatchOrderStatus.Terminated),
            Monthly    = monthly,
            Details    = details
        };
    }

    public async Task<byte[]> ExportRentalExcelAsync(DateOnly from, DateOnly to)
    {
        var stats = await GetRentalStatsAsync(from, to);

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("租赁统计");

        string[] headers = ["项目名称", "设备名称", "开始日期", "结束日期", "租期(天)", "估算收入(元)", "状态"];
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cells[1, c + 1].Value = headers[c];
            ws.Cells[1, c + 1].Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var d in stats.Details)
        {
            int days = Math.Max(0, d.ActualEnd.DayNumber - d.ActualStart.DayNumber);
            ws.Cells[row, 1].Value = d.ProjectName;
            ws.Cells[row, 2].Value = d.EquipmentName;
            ws.Cells[row, 3].Value = d.ActualStart.ToString("yyyy-MM-dd");
            ws.Cells[row, 4].Value = d.ActualEnd.ToString("yyyy-MM-dd");
            ws.Cells[row, 5].Value = days;
            ws.Cells[row, 6].Value = d.EstimatedRevenue;
            ws.Cells[row, 6].Style.Numberformat.Format = "#,##0.00";
            ws.Cells[row, 7].Value = d.Status;
            row++;
        }

        if (ws.Dimension != null)
            ws.Cells[ws.Dimension.Address].AutoFitColumns();

        return package.GetAsByteArray();
    }

    // ── Utilization ───────────────────────────────────────────────────────

    public async Task<UtilizationDto> GetUtilizationAsync(DateOnly from, DateOnly to)
    {
        int totalDays = to.DayNumber - from.DayNumber + 1;

        // Load categories with their equipments and relevant orders
        var categories = await _db.EquipmentCategories
            .Include(c => c.Equipments)
                .ThenInclude(e => e.DispatchOrders
                    .Where(o => o.Status == DispatchOrderStatus.InProgress
                             || o.Status == DispatchOrderStatus.Complete
                             || o.Status == DispatchOrderStatus.Signed))
            .Where(c => c.Equipments.Any())
            .ToListAsync();

        var result = categories
            .Select(c =>
            {
                int eqCount      = c.Equipments.Count;
                int totalAvail   = eqCount * totalDays;
                int occupiedDays = c.Equipments.Sum(e =>
                    e.DispatchOrders
                        .Where(o => o.ActualStart <= to && o.ActualEnd >= from)
                        .Sum(o =>
                        {
                            var start = o.ActualStart.DayNumber < from.DayNumber ? from.DayNumber : o.ActualStart.DayNumber;
                            var end   = o.ActualEnd.DayNumber   > to.DayNumber   ? to.DayNumber   : o.ActualEnd.DayNumber;
                            return Math.Max(0, end - start + 1);
                        }));

                return new CategoryUtilizationDto
                {
                    CategoryName   = c.Name,
                    EquipmentCount = eqCount,
                    TotalDays      = totalAvail,
                    OccupiedDays   = occupiedDays
                };
            })
            .OrderByDescending(c => c.UtilizationRate)
            .ToList();

        return new UtilizationDto { Categories = result };
    }

    // ── Safety Records ────────────────────────────────────────────────────

    public async Task<SafetyStatsDto> GetSafetyStatsAsync(DateOnly from, DateOnly to)
    {
        var briefings = await _db.SafetyBriefings
            .Where(b => b.BriefingDate >= from && b.BriefingDate <= to)
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var inspections = await _db.InspectionRecords
            .Where(i => i.InspectionDate >= from && i.InspectionDate <= to)
            .GroupBy(i => i.OverallStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt   = to.ToDateTime(new TimeOnly(23, 59, 59));

        var faultsBySeverity = await _db.FaultReports
            .Where(f => f.ReportedAt >= fromDt && f.ReportedAt <= toDt)
            .GroupBy(f => f.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToListAsync();

        var faultsByStatus = await _db.FaultReports
            .Where(f => f.ReportedAt >= fromDt && f.ReportedAt <= toDt)
            .GroupBy(f => f.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return new SafetyStatsDto
        {
            BriefingStatus = briefings.ToDictionary(
                x => x.Status == SafetyBriefingStatus.Draft ? "草稿" : "已完成",
                x => x.Count),
            InspectionStatus = inspections.ToDictionary(
                x => x.Status == OverallInspectionStatus.Normal ? "正常" : "异常",
                x => x.Count),
            FaultSeverity = faultsBySeverity.ToDictionary(
                x => x.Severity == FaultSeverity.Minor ? "轻微"
                   : x.Severity == FaultSeverity.Medium ? "中等" : "严重",
                x => x.Count),
            FaultStatus = faultsByStatus.ToDictionary(
                x => x.Status == FaultStatus.Pending ? "待处理"
                   : x.Status == FaultStatus.InProgress ? "处理中" : "已关闭",
                x => x.Count)
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string OrderStatusToString(DispatchOrderStatus s) => s switch
    {
        DispatchOrderStatus.Unsigned   => "待签约",
        DispatchOrderStatus.Signed     => "已签约",
        DispatchOrderStatus.InProgress => "租赁中",
        DispatchOrderStatus.Complete   => "已完成",
        DispatchOrderStatus.Terminated => "已终止",
        _                              => s.ToString()
    };
}
