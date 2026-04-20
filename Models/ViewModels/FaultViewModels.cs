using System.ComponentModel.DataAnnotations;
using EquipmentRental.Models;

namespace EquipmentRental.Models.ViewModels;

public class CreateFaultViewModel
{
    public int OrderId { get; set; }

    // Display-only — re-populated on validation fail
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "请填写故障描述")]
    [StringLength(500, ErrorMessage = "故障描述不超过 500 字")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "请选择严重程度")]
    public FaultSeverity Severity { get; set; }

    public IList<IFormFile>? Images { get; set; }
}

public class FaultDetailViewModel
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FaultSeverity Severity { get; set; }
    public string ReporterName { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public FaultStatus Status { get; set; }
    public string? Resolution { get; set; }
    public decimal? RepairCost { get; set; }
    public string? ClosedByName { get; set; }
    public DateTime? ClosedAt { get; set; }
    public IList<FaultImageViewModel> Images { get; set; } = [];

    // Status flags (role check done in view with User.IsInRole)
    public bool CanAccept { get; set; }
    public bool CanClose { get; set; }
}

public class FaultImageViewModel
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class CloseFaultViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "请填写处理说明")]
    [StringLength(500, ErrorMessage = "处理说明不超过 500 字")]
    public string Resolution { get; set; } = string.Empty;

    [Required(ErrorMessage = "请填写维修费用")]
    [Range(0, 9999999.99, ErrorMessage = "维修费用范围 0 ~ 9,999,999.99")]
    public decimal RepairCost { get; set; }
}

public class FaultListViewModel
{
    public IList<FaultListItemViewModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public IList<InProgressOrderOptionViewModel> AvailableOrders { get; set; } = [];
}

public class FaultListItemViewModel
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;
    public FaultSeverity Severity { get; set; }
    public string ReporterName { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
    public FaultStatus Status { get; set; }
}
