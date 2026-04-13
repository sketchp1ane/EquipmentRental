using Microsoft.AspNetCore.Identity;

namespace EquipmentRental.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string RealName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
