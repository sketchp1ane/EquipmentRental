namespace EquipmentRental.Models.ViewModels;

public class ExpiringCertViewModel
{
    public int EquipmentId { get; set; }
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string CertTypeName { get; set; } = string.Empty;
    public DateOnly ValidTo { get; set; }
    public int DaysLeft { get; set; }
    public bool IsExpired { get; set; }
}

public class HomeIndexViewModel
{
    public IList<ExpiringCertViewModel> ExpiringCerts { get; set; } = [];
}
