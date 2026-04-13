using EquipmentRental.Models;

namespace EquipmentRental.Models.Entities;

public class Contract
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ContractNo { get; set; } = string.Empty;
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    public string? ScanPath { get; set; }
    public DateTime CreatedAt { get; set; }

    public DispatchOrder Order { get; set; } = null!;
}
