using EquipmentRental.Models;

namespace EquipmentRental.Models.Entities;

public class AuditRecord
{
    public int Id { get; set; }
    public int EquipmentId { get; set; }
    public string AuditorId { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string? Remark { get; set; }
    public DateTime AuditedAt { get; set; }

    public Equipment Equipment { get; set; } = null!;
    public ApplicationUser Auditor { get; set; } = null!;
}
