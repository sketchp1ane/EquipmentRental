using EquipmentRental.Models;

namespace EquipmentRental.Models.Entities;

public class Qualification
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public QualificationType Type { get; set; }
    public string? CertNo { get; set; }
    public string? IssuedBy { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }
    public string? FilePath { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Equipment Equipment { get; set; } = null!;
}
