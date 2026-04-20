namespace EquipmentRental.Models.ViewModels;

public record MonthlyCountDto(string Month, int Count, decimal Revenue);

public class DashboardStatsDto
{
    public int EquipmentTotal         { get; set; }
    public int EquipmentPendingReview { get; set; }
    public int EquipmentIdle          { get; set; }
    public int EquipmentInUse         { get; set; }
    public int EquipmentMaintenance   { get; set; }
    public int EquipmentScrapped      { get; set; }
    public int ActiveOrders           { get; set; }
    public List<MonthlyCountDto> MonthlyTrend { get; set; } = [];
}

public class RentalDetailDto
{
    public string   ProjectName       { get; set; } = string.Empty;
    public string   EquipmentName     { get; set; } = string.Empty;
    public DateOnly ActualStart       { get; set; }
    public DateOnly ActualEnd         { get; set; }
    public decimal  EstimatedRevenue  { get; set; }
    public string   Status            { get; set; } = string.Empty;
}

public class RentalStatsDto
{
    public int Total      { get; set; }
    public int Completed  { get; set; }
    public int InProgress { get; set; }
    public int Terminated { get; set; }
    public List<MonthlyCountDto> Monthly { get; set; } = [];
    public List<RentalDetailDto> Details { get; set; } = [];
}

public class RentalReportViewModel
{
    public DateOnly      From  { get; set; }
    public DateOnly      To    { get; set; }
    public RentalStatsDto Stats { get; set; } = new();
}

public class CategoryUtilizationDto
{
    public string CategoryName   { get; set; } = string.Empty;
    public int    EquipmentCount { get; set; }
    public int    TotalDays      { get; set; }
    public int    OccupiedDays   { get; set; }
    public double UtilizationRate => TotalDays > 0
        ? Math.Round((double)OccupiedDays / TotalDays * 100, 1) : 0;
}

public class UtilizationDto
{
    public List<CategoryUtilizationDto> Categories { get; set; } = [];
}

public class UtilizationViewModel
{
    public DateOnly      From  { get; set; }
    public DateOnly      To    { get; set; }
    public UtilizationDto Stats { get; set; } = new();
}

public class SafetyStatsDto
{
    public Dictionary<string, int> BriefingStatus  { get; set; } = [];
    public Dictionary<string, int> InspectionStatus { get; set; } = [];
    public Dictionary<string, int> FaultSeverity   { get; set; } = [];
    public Dictionary<string, int> FaultStatus     { get; set; } = [];
}

public class SafetyReportViewModel
{
    public DateOnly      From  { get; set; }
    public DateOnly      To    { get; set; }
    public SafetyStatsDto Stats { get; set; } = new();
}
