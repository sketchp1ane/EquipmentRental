namespace EquipmentRental.Models.ViewModels;

/// <summary>
/// Lightweight attachment DTO consumed by _AttachmentList partial.
/// </summary>
public class AttachmentVm
{
    public string Url { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? DeleteUrl { get; set; }
    public string? DeleteFormAction { get; set; }
    public IDictionary<string, string>? DeleteFormFields { get; set; }
    public bool IsImage { get; set; }
    public string? CreatedAt { get; set; }
}
