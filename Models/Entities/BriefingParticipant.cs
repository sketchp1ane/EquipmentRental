namespace EquipmentRental.Models.Entities;

public class BriefingParticipant
{
    public int Id { get; set; }
    public int BriefingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? SignedById { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? ClientIp { get; set; }

    public SafetyBriefing Briefing { get; set; } = null!;
    public ApplicationUser? SignedBy { get; set; }
}
