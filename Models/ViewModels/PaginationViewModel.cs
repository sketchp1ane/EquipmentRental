namespace EquipmentRental.Models.ViewModels;

public class PaginationViewModel
{
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public string? ActionName { get; set; }
    public IDictionary<string, string?>? RouteValues { get; set; }
}
