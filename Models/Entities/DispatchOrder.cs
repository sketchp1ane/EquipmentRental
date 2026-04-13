using EquipmentRental.Models;

namespace EquipmentRental.Models.Entities;

public class DispatchOrder
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public int EquipmentId { get; set; }
    public string DispatcherId { get; set; } = string.Empty;
    public DateOnly ActualStart { get; set; }
    public DateOnly ActualEnd { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Deposit { get; set; }
    public string VerifyCode { get; set; } = string.Empty;
    public DispatchOrderStatus Status { get; set; } = DispatchOrderStatus.Unsigned;
    public DateTime CreatedAt { get; set; }

    public DispatchRequest Request { get; set; } = null!;
    public Equipment Equipment { get; set; } = null!;
    public ApplicationUser Dispatcher { get; set; } = null!;

    // 1:1 inverse navigation
    public Contract? Contract { get; set; }
    public EntryVerification? EntryVerification { get; set; }
    public ReturnApplication? ReturnApplication { get; set; }

    public ICollection<SafetyBriefing> SafetyBriefings { get; set; } = new List<SafetyBriefing>();
    public ICollection<InspectionRecord> InspectionRecords { get; set; } = new List<InspectionRecord>();
    public ICollection<FaultReport> FaultReports { get; set; } = new List<FaultReport>();
}
