using System.ComponentModel.DataAnnotations;
using EquipmentRental.Models;

namespace EquipmentRental.Models.ViewModels;

public class ReturnListViewModel
{
    public IList<ReturnListItemViewModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public IList<InProgressOrderOptionViewModel> AvailableOrders { get; set; } = [];
}

public class ReturnListItemViewModel
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public DateOnly ActualReturnDate { get; set; }
    public ReturnApplicationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApplyReturnViewModel
{
    public int OrderId { get; set; }

    // Display-only — re-populated on validation fail
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;
    public decimal Deposit { get; set; }

    [Required(ErrorMessage = "请填写实际退场日期")]
    public DateOnly ActualReturnDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(500, ErrorMessage = "设备状况描述不超过 500 字")]
    public string? ConditionDesc { get; set; }
}

public class ReturnDetailViewModel
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string ApplicantName { get; set; } = string.Empty;
    public DateOnly ActualReturnDate { get; set; }
    public string? ConditionDesc { get; set; }
    public ReturnApplicationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Deposit { get; set; }

    // Evaluation (null until DeviceAdmin fills it)
    public ReturnEvaluationDetailViewModel? Evaluation { get; set; }

    public bool CanEvaluate { get; set; }
}

public class ReturnEvaluationDetailViewModel
{
    public int Id { get; set; }
    public string EvaluatorName { get; set; } = string.Empty;
    public int AppearanceScore { get; set; }
    public int FunctionScore { get; set; }
    public string? DamageDesc { get; set; }
    public decimal Deduction { get; set; }
    public decimal RefundAmount { get; set; }
    public string? Remark { get; set; }
    public DateTime EvaluatedAt { get; set; }
    public string NewEquipmentStatus { get; set; } = string.Empty;
}

public class EvaluateReturnViewModel
{
    public int ReturnAppId { get; set; }

    // Display-only — re-populated on validation fail
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;
    public decimal Deposit { get; set; }

    [Required(ErrorMessage = "请选择外观评分")]
    [Range(1, 5, ErrorMessage = "外观评分范围 1 ~ 5")]
    public int AppearanceScore { get; set; }

    [Required(ErrorMessage = "请选择功能评分")]
    [Range(1, 5, ErrorMessage = "功能评分范围 1 ~ 5")]
    public int FunctionScore { get; set; }

    [StringLength(500, ErrorMessage = "损耗描述不超过 500 字")]
    public string? DamageDesc { get; set; }

    [Required(ErrorMessage = "请填写损耗扣款金额（无损耗填 0）")]
    [Range(0, 9999999.99, ErrorMessage = "扣款金额范围 0 ~ 9,999,999.99")]
    public decimal Deduction { get; set; }

    // Calculated server-side; client also shows it for UX
    public decimal RefundAmount { get; set; }

    [StringLength(500, ErrorMessage = "综合评价备注不超过 500 字")]
    public string? Remark { get; set; }

    [Required(ErrorMessage = "请选择退场后设备状态")]
    public EquipmentStatus NewEquipmentStatus { get; set; } = EquipmentStatus.Idle;
}
