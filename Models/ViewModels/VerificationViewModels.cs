using System.ComponentModel.DataAnnotations;
using EquipmentRental.Models;

namespace EquipmentRental.Models.ViewModels;

// ── 核验表单（含订单预览） ────────────────────────────────────────────────────

public class VerifyFormViewModel
{
    [Required(ErrorMessage = "请输入核验码")]
    [StringLength(36, MinimumLength = 36, ErrorMessage = "核验码格式不正确")]
    public string? VerifyCode { get; set; }

    // 预览区（核验码有效时填充）
    public bool HasOrderInfo { get; set; }
    public int OrderId { get; set; }
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly ActualStart { get; set; }
    public DateOnly ActualEnd { get; set; }
    public DispatchOrderStatus OrderStatus { get; set; }
}

// ── 核验结果详情 ──────────────────────────────────────────────────────────────

public class VerificationDetailViewModel
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public DateOnly ActualStart { get; set; }
    public DateOnly ActualEnd { get; set; }
    public string VerifierName { get; set; } = string.Empty;
    public DateTime VerifiedAt { get; set; }
    public bool IsPass { get; set; }
    public string? FailReason { get; set; }
}

// ── 核验列表 ──────────────────────────────────────────────────────────────────

public class VerificationListViewModel
{
    public IList<VerificationListItemViewModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class VerificationListItemViewModel
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string EquipmentNo { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string VerifierName { get; set; } = string.Empty;
    public DateTime VerifiedAt { get; set; }
    public bool IsPass { get; set; }
}
