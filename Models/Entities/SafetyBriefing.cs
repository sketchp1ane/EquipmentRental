using EquipmentRental.Models;

namespace EquipmentRental.Models.Entities;

public class SafetyBriefing
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string CreatorId { get; set; } = string.Empty;
    public DateOnly BriefingDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public SafetyBriefingStatus Status { get; set; } = SafetyBriefingStatus.Draft;
    public DateTime CreatedAt { get; set; }

    public DispatchOrder Order { get; set; } = null!;
    public ApplicationUser Creator { get; set; } = null!;
    public ICollection<BriefingParticipant> Participants { get; set; } = new List<BriefingParticipant>();
}
