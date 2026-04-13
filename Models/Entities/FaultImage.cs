namespace EquipmentRental.Models.Entities;

public class FaultImage
{
    public int Id { get; set; }
    public int FaultReportId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }

    public FaultReport FaultReport { get; set; } = null!;
}
