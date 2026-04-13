using EquipmentRental.Models;

namespace EquipmentRental.Models.Entities;

public class Equipment
{
    public int Id { get; set; }
    public string EquipmentNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string BrandModel { get; set; } = string.Empty;
    public DateOnly ManufactureDate { get; set; }
    public string? FactoryNo { get; set; }
    public string? TechSpecs { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public decimal? OriginalValue { get; set; }
    public string OwnedBy { get; set; } = string.Empty;
    public EquipmentStatus Status { get; set; } = EquipmentStatus.PendingReview;
    public string? Remark { get; set; }
    public string CreatedById { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public EquipmentCategory Category { get; set; } = null!;
    public ApplicationUser CreatedBy { get; set; } = null!;
    public ICollection<EquipmentImage> Images { get; set; } = new List<EquipmentImage>();
    public ICollection<Qualification> Qualifications { get; set; } = new List<Qualification>();
    public ICollection<AuditRecord> AuditRecords { get; set; } = new List<AuditRecord>();
    public ICollection<DispatchOrder> DispatchOrders { get; set; } = new List<DispatchOrder>();
    public ICollection<InspectionRecord> InspectionRecords { get; set; } = new List<InspectionRecord>();
    public ICollection<FaultReport> FaultReports { get; set; } = new List<FaultReport>();
}
