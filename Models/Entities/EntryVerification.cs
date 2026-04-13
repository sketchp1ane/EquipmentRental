namespace EquipmentRental.Models.Entities;

public class EntryVerification
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string VerifierId { get; set; } = string.Empty;
    public DateTime VerifiedAt { get; set; }
    public bool IsPass { get; set; }
    public string? FailReason { get; set; }

    public DispatchOrder Order { get; set; } = null!;
    public ApplicationUser Verifier { get; set; } = null!;
}
