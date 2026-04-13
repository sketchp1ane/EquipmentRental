namespace EquipmentRental.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string DeviceAdmin = "DeviceAdmin";
    public const string Dispatcher = "Dispatcher";
    public const string ProjectLead = "ProjectLead";
    public const string SafetyOfficer = "SafetyOfficer";
    public const string Auditor = "Auditor";

    public static readonly string[] All =
        [Admin, DeviceAdmin, Dispatcher, ProjectLead, SafetyOfficer, Auditor];
}
