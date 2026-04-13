namespace EquipmentRental.Models.Entities;

public class EquipmentImage
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }

    public Equipment Equipment { get; set; } = null!;
}
