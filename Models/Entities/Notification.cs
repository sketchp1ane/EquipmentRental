namespace EquipmentRental.Models.Entities;

public class Notification
{
    public int Id { get; set; }
    public string RecipientId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string? RelatedUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public ApplicationUser Recipient { get; set; } = null!;
}
