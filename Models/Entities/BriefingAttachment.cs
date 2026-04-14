namespace EquipmentRental.Models.Entities;

public class BriefingAttachment
{
    public int Id { get; set; }
    public int BriefingId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;

    public SafetyBriefing Briefing { get; set; } = null!;
}
