namespace EquipmentRental.Models.Entities;

public class InspectionImage
{
    public int Id { get; set; }
    public int InspectionId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }

    public InspectionRecord InspectionRecord { get; set; } = null!;
}
