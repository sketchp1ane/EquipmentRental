using EquipmentRental.Models;

namespace EquipmentRental.Models.Entities;

public class InspectionItemResult
{
    public int Id { get; set; }
    public int InspectionId { get; set; }
    public string ItemKey { get; set; } = string.Empty;
    public InspectionItemStatus Status { get; set; }
    public string? Remark { get; set; }

    public InspectionRecord InspectionRecord { get; set; } = null!;
}
