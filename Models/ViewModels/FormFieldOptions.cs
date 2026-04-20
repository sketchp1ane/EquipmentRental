namespace EquipmentRental.Models.ViewModels;

public class FormFieldOptions
{
    public string For { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "text";
    public bool Required { get; set; }
    public string? Help { get; set; }
    public string? Placeholder { get; set; }
    public string? Value { get; set; }
    public int? MaxLength { get; set; }
    public int? Rows { get; set; }
}
