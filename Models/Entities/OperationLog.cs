namespace EquipmentRental.Models.Entities;

public class OperationLog
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? ClientIp { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
