namespace EquipmentRental.Constants;

public static class InspectionChecklist
{
    public sealed record Item(string Key, string Name, int Order);

    public static readonly IReadOnlyList<Item> Standard = new[]
    {
        new Item("appearance",           "外观完好性",       1),
        new Item("qualifications",       "证件齐全",         2),
        new Item("guards",               "防护装置",         3),
        new Item("emergency_brake",      "紧急制动",         4),
        new Item("hydraulic_electrical", "液压/电气系统",    5),
        new Item("surroundings",         "周边环境",         6),
        new Item("operator_cert",        "操作员持证",       7),
        new Item("safety_signs",         "安全标识",         8),
    };

    public static string NameOf(string key) =>
        Standard.FirstOrDefault(i => i.Key == key)?.Name ?? key;
}
