using EquipmentRental.Models;

namespace EquipmentRental.Models.Entities;

public class FaultReport
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public int OrderId { get; set; }
    public string ReporterId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FaultSeverity Severity { get; set; }
    public DateTime ReportedAt { get; set; }
    public FaultStatus Status { get; set; } = FaultStatus.Pending;
    public string? Resolution { get; set; }
    public decimal? RepairCost { get; set; }
    public string? ClosedById { get; set; }
    public DateTime? ClosedAt { get; set; }

    public Equipment Equipment { get; set; } = null!;
    public DispatchOrder Order { get; set; } = null!;
    public ApplicationUser Reporter { get; set; } = null!;
    public ApplicationUser? ClosedBy { get; set; }
    public ICollection<FaultImage> Images { get; set; } = new List<FaultImage>();
}
