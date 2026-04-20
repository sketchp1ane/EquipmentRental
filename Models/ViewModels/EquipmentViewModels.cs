using System.ComponentModel.DataAnnotations;
using EquipmentRental.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EquipmentRental.Models.ViewModels;

// ── Equipment List ViewModels ─────────────────────────────────────────────────

public class EquipmentListViewModel
{
    public IList<EquipmentListItemViewModel> Items { get; set; } = [];
    public int? CategoryId { get; set; }
    public EquipmentStatus? Status { get; set; }
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public IList<SelectListItem> CategoryOptions { get; set; } = [];
}

public class EquipmentListItemViewModel
{
    public int Id { get; set; }
    public string EquipmentNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BrandModel { get; set; } = string.Empty;
    public EquipmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Equipment Create/Edit ViewModels ──────────────────────────────────────────

public class CreateEquipmentViewModel
{
    [Required(ErrorMessage = "请输入设备编号")]
    [StringLength(50, ErrorMessage = "设备编号不能超过50个字符")]
    [Display(Name = "设备编号")]
    public string EquipmentNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入设备名称")]
    [StringLength(100, ErrorMessage = "设备名称不能超过100个字符")]
    [Display(Name = "设备名称")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "请选择设备分类")]
    [Display(Name = "设备分类")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "请输入品牌/型号")]
    [StringLength(100, ErrorMessage = "品牌/型号不能超过100个字符")]
    [Display(Name = "品牌/型号")]
    public string BrandModel { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入出厂日期")]
    [Display(Name = "出厂日期")]
    public DateOnly ManufactureDate { get; set; }

    [StringLength(100, ErrorMessage = "出厂编号不能超过100个字符")]
    [Display(Name = "出厂编号")]
    public string? FactoryNo { get; set; }

    [StringLength(500, ErrorMessage = "技术参数不能超过500个字符")]
    [Display(Name = "技术参数")]
    public string? TechSpecs { get; set; }

    [Display(Name = "购置日期")]
    public DateOnly? PurchaseDate { get; set; }

    [Range(0, 999999999.99, ErrorMessage = "原值格式不正确")]
    [Display(Name = "原值(元)")]
    public decimal? OriginalValue { get; set; }

    [Required(ErrorMessage = "请输入所属单位")]
    [StringLength(100, ErrorMessage = "所属单位不能超过100个字符")]
    [Display(Name = "所属单位")]
    public string OwnedBy { get; set; } = string.Empty;

    [Display(Name = "状态")]
    public EquipmentStatus Status { get; set; } = EquipmentStatus.PendingReview;

    [StringLength(500, ErrorMessage = "备注不能超过500个字符")]
    [Display(Name = "备注")]
    public string? Remark { get; set; }

    [Display(Name = "设备图片")]
    public IFormFileCollection? Images { get; set; }

    public IList<SelectListItem> CategoryOptions { get; set; } = [];
}

public class EditEquipmentViewModel : CreateEquipmentViewModel
{
    public int Id { get; set; }
    public IList<EquipmentImageViewModel> ExistingImages { get; set; } = [];
    public List<int> DeleteImageIds { get; set; } = [];
}

public class EquipmentImageViewModel
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

// ── Equipment Details ViewModel ───────────────────────────────────────────────

public class EquipmentDetailsViewModel
{
    public int Id { get; set; }
    public string EquipmentNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string BrandModel { get; set; } = string.Empty;
    public DateOnly ManufactureDate { get; set; }
    public string? FactoryNo { get; set; }
    public string? TechSpecs { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public decimal? OriginalValue { get; set; }
    public string OwnedBy { get; set; } = string.Empty;
    public EquipmentStatus Status { get; set; }
    public string? Remark { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public IList<EquipmentImageViewModel> Images { get; set; } = [];
}
