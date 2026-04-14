using System.ComponentModel.DataAnnotations;
using EquipmentRental.Models;

namespace EquipmentRental.Models.ViewModels;

public class AuditListViewModel
{
    public IList<AuditListItemViewModel> Items { get; set; } = [];
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public string? Keyword { get; set; }
}

public class AuditListItemViewModel
{
    public int EquipmentId { get; set; }
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int QualificationCount { get; set; }
}

public class AuditDetailViewModel
{
    public int EquipmentId { get; set; }
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BrandModel { get; set; } = string.Empty;
    public string OwnedBy { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public IList<QualificationListItemViewModel> Qualifications { get; set; } = [];
    public AuditActionViewModel Form { get; set; } = new();
}

public class AuditActionViewModel
{
    [Required]
    public int EquipmentId { get; set; }

    [Required(ErrorMessage = "请选择审核结论")]
    [Display(Name = "审核结论")]
    public AuditAction Action { get; set; }

    [StringLength(500)]
    [Display(Name = "备注 / 驳回原因")]
    public string? Remark { get; set; }
}
