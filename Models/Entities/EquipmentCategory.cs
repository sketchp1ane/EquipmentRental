namespace EquipmentRental.Models.Entities;

public class EquipmentCategory
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int SortOrder { get; set; }

    public EquipmentCategory? Parent { get; set; }
    public ICollection<EquipmentCategory> Children { get; set; } = new List<EquipmentCategory>();
    public ICollection<Equipment> Equipments { get; set; } = new List<Equipment>();
}
