using System.ComponentModel.DataAnnotations;
using EquipmentRental.Models;
using Microsoft.AspNetCore.Http;

namespace EquipmentRental.Models.ViewModels;

public class QualificationIndexViewModel
{
    public int EquipmentId { get; set; }
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public EquipmentStatus EquipmentStatus { get; set; }
    public IList<QualificationListItemViewModel> Items { get; set; } = [];
}

public class QualificationListItemViewModel
{
    public int Id { get; set; }
    public QualificationType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? CertNo { get; set; }
    public string? IssuedBy { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }
    public bool IsExpired { get; set; }
    public bool IsExpiringSoon { get; set; }
    public string? FilePath { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class QualificationFormViewModel
{
    public int Id { get; set; }  // 0 = create

    [Required]
    public int EquipmentId { get; set; }

    [Required(ErrorMessage = "请选择证件类型")]
    [Display(Name = "证件类型")]
    public QualificationType Type { get; set; }

    [StringLength(100)]
    [Display(Name = "证件编号")]
    public string? CertNo { get; set; }

    [StringLength(100)]
    [Display(Name = "发证机构")]
    public string? IssuedBy { get; set; }

    [Required(ErrorMessage = "请填写有效期起始日期")]
    [Display(Name = "有效期起")]
    public DateOnly ValidFrom { get; set; }

    [Required(ErrorMessage = "请填写有效期截止日期")]
    [Display(Name = "有效期至")]
    public DateOnly ValidTo { get; set; }

    [Display(Name = "证件附件")]
    public IFormFile? CertFile { get; set; }

    public string? ExistingFilePath { get; set; }

    [Display(Name = "删除已有附件")]
    public bool RemoveFile { get; set; }

    // Display-only
    public string EquipmentName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;
}
