using EquipmentRental.Models;

namespace EquipmentRental.Models.Entities;

public class DispatchRequest
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectAddress { get; set; } = string.Empty;
    public string RequesterId { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int Quantity { get; set; }
    public DateOnly ExpectedStart { get; set; }
    public DateOnly ExpectedEnd { get; set; }
    public string? SpecialRequirements { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public DispatchRequestStatus Status { get; set; } = DispatchRequestStatus.Pending;
    public DateTime CreatedAt { get; set; }

    public ApplicationUser Requester { get; set; } = null!;
    public EquipmentCategory Category { get; set; } = null!;
    public ICollection<DispatchOrder> Orders { get; set; } = new List<DispatchOrder>();
}
