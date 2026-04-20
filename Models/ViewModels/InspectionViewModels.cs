using System.ComponentModel.DataAnnotations;
using EquipmentRental.Models;

namespace EquipmentRental.Models.ViewModels;

public class CreateInspectionViewModel
{
    public int OrderId { get; set; }

    // Display-only — re-populated on validation fail
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "请选择巡检日期")]
    public DateOnly InspectionDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "请选择总体状态")]
    public OverallInspectionStatus OverallStatus { get; set; }

    [StringLength(500, ErrorMessage = "备注不超过 500 字")]
    public string? Remark { get; set; }

    public IList<IFormFile>? Images { get; set; }
}

public class InspectionDetailViewModel
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public DateOnly InspectionDate { get; set; }
    public OverallInspectionStatus OverallStatus { get; set; }
    public string? Remark { get; set; }
    public string InspectorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public IList<InspectionImageViewModel> Images { get; set; } = [];
}

public class InspectionImageViewModel
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class InspectionListViewModel
{
    public IList<InspectionListItemViewModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public IList<InProgressOrderOptionViewModel> AvailableOrders { get; set; } = [];
}

public class InspectionListItemViewModel
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;
    public DateOnly InspectionDate { get; set; }
    public OverallInspectionStatus OverallStatus { get; set; }
    public string InspectorName { get; set; } = string.Empty;
}
