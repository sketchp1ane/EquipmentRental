using System.ComponentModel.DataAnnotations;
using EquipmentRental.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EquipmentRental.Models.ViewModels;

// ── 用车申请表单 ──────────────────────────────────────────────────────────────
public class DispatchRequestFormViewModel
{
    [Required(ErrorMessage = "项目名称必填")]
    [StringLength(100)]
    public string ProjectName { get; set; } = string.Empty;

    [Required(ErrorMessage = "项目地址必填")]
    [StringLength(200)]
    public string ProjectAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "请选择设备类型")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "数量必填")]
    [Range(1, 100, ErrorMessage = "数量须在 1–100 之间")]
    public int Quantity { get; set; } = 1;

    [Required(ErrorMessage = "预计开始日期必填")]
    public DateOnly ExpectedStart { get; set; }

    [Required(ErrorMessage = "预计归还日期必填")]
    public DateOnly ExpectedEnd { get; set; }

    [StringLength(500)]
    public string? SpecialRequirements { get; set; }

    [Required(ErrorMessage = "联系人必填")]
    [StringLength(50)]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "联系电话必填")]
    [StringLength(20)]
    [Phone(ErrorMessage = "联系电话格式不正确")]
    public string ContactPhone { get; set; } = string.Empty;

    public IList<SelectListItem> CategoryOptions { get; set; } = [];
}

// ── 调度列表 ──────────────────────────────────────────────────────────────────
public class DispatchListViewModel
{
    public IList<DispatchRequestListItemViewModel> Items { get; set; } = [];
    public DispatchRequestStatus? StatusFilter { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class DispatchRequestListItemViewModel
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateOnly ExpectedStart { get; set; }
    public DateOnly ExpectedEnd { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public DispatchRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrderCount { get; set; }
}

// ── 可用设备（调度排期用） ─────────────────────────────────────────────────────
public class AvailableEquipmentViewModel
{
    public int Id { get; set; }
    public string EquipmentNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BrandModel { get; set; } = string.Empty;
    public string OwnedBy { get; set; } = string.Empty;
}

// ── 创建调度单表单 ────────────────────────────────────────────────────────────
public class CreateOrderViewModel
{
    // 只读：申请信息
    public int RequestId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int Quantity { get; set; }
    public DateOnly ExpectedStart { get; set; }
    public DateOnly ExpectedEnd { get; set; }
    public string? SpecialRequirements { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;

    // 表单输入
    [Required(ErrorMessage = "请选择设备")]
    public int EquipmentId { get; set; }

    [Required(ErrorMessage = "实际开始日期必填")]
    public DateOnly ActualStart { get; set; }

    [Required(ErrorMessage = "实际结束日期必填")]
    public DateOnly ActualEnd { get; set; }

    [Required(ErrorMessage = "日租金必填")]
    [Range(0.01, 9999999, ErrorMessage = "日租金须大于 0")]
    public decimal UnitPrice { get; set; }

    [Required(ErrorMessage = "押金必填")]
    [Range(0, 9999999, ErrorMessage = "押金须不小于 0")]
    public decimal Deposit { get; set; }

    // 可用设备列表
    public IList<AvailableEquipmentViewModel> AvailableEquipments { get; set; } = [];
}

// ── 调度单列表 ────────────────────────────────────────────────────────────────
public class DispatchOrderListViewModel
{
    public IList<DispatchOrderListItemViewModel> Items { get; set; } = [];
    public DispatchOrderStatus? StatusFilter { get; set; }
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class DispatchOrderListItemViewModel
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public DateOnly ActualStart { get; set; }
    public DateOnly ActualEnd { get; set; }
    public decimal UnitPrice { get; set; }
    public DispatchOrderStatus Status { get; set; }
    public string? ContractNo { get; set; }
    public ContractStatus? ContractStatus { get; set; }
    public string DispatcherName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ── 调度单详情 ────────────────────────────────────────────────────────────────
public class OrderDetailViewModel
{
    public int OrderId { get; set; }
    public int RequestId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectAddress { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string BrandModel { get; set; } = string.Empty;
    public DateOnly ActualStart { get; set; }
    public DateOnly ActualEnd { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Deposit { get; set; }
    public decimal RentalAmount { get; set; }
    public string VerifyCode { get; set; } = string.Empty;
    public string DispatcherName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public DispatchOrderStatus OrderStatus { get; set; }
    public int? ContractId { get; set; }
    public ContractStatus? ContractStatus { get; set; }
    public string? ContractNo { get; set; }
    public DateTime CreatedAt { get; set; }
    public string QrCodeDataUri { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
}

// ── 日历页面 Shell ────────────────────────────────────────────────────────────
public class CalendarPageViewModel
{
    public DateOnly RangeStart { get; set; }
    public DateOnly RangeEnd { get; set; }
}

// ── 进行中调度单选项（供巡检/故障/退场下拉使用）────────────────────────────────
public class InProgressOrderOptionViewModel
{
    public int OrderId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
}
