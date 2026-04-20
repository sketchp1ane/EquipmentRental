using EquipmentRental.Models;

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

public class ExpiringCertViewModel
{
    public int EquipmentId { get; set; }
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string CertTypeName { get; set; } = string.Empty;
    public DateOnly ValidTo { get; set; }
    public int DaysLeft { get; set; }
    public bool IsExpired { get; set; }
}

public class HomeIndexViewModel
{
    public IList<ExpiringCertViewModel> ExpiringCerts { get; set; } = [];
    public DashboardStatsDto? Stats { get; set; }
    public IList<PendingActionViewModel> PendingActions { get; set; } = [];
}

public record PendingActionViewModel(string Title, int Count, string Url, string Color);
