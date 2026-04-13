using EquipmentRental.Models;

namespace EquipmentRental.Models.Entities;

public class InspectionRecord
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public int OrderId { get; set; }
    public string InspectorId { get; set; } = string.Empty;
    public DateOnly InspectionDate { get; set; }
    public OverallInspectionStatus OverallStatus { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }

    public Equipment Equipment { get; set; } = null!;
    public DispatchOrder Order { get; set; } = null!;
    public ApplicationUser Inspector { get; set; } = null!;
    public ICollection<InspectionImage> Images { get; set; } = new List<InspectionImage>();
}
