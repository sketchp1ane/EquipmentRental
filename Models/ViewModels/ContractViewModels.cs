using System.ComponentModel.DataAnnotations;
using EquipmentRental.Models;

namespace EquipmentRental.Models.ViewModels;

// ── 合同详情（HTML 预览 + PDF 共用） ─────────────────────────────────────────
public class ContractDetailViewModel
{
    public int ContractId { get; set; }
    public int OrderId { get; set; }
    public string ContractNo { get; set; } = string.Empty;
    public ContractStatus Status { get; set; }

    // 甲方（租赁公司）
    public string PartyAName { get; set; } = string.Empty;

    // 乙方（项目负责人 / 申请人）
    public string PartyBName { get; set; } = string.Empty;
    public string PartyBPhone { get; set; } = string.Empty;

    // 项目信息
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectAddress { get; set; } = string.Empty;

    // 设备信息
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string BrandModel { get; set; } = string.Empty;
    public string OwnedBy { get; set; } = string.Empty;

    // 租期与费用
    public DateOnly RentalStart { get; set; }
    public DateOnly RentalEnd { get; set; }
    public int RentalDays { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal RentalAmount { get; set; }
    public decimal Deposit { get; set; }

    // 违约条款（模板文本，由 service 注入）
    public string ViolationClauses { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public string? ScanPath { get; set; }
}

// ── 上传扫描件表单 ────────────────────────────────────────────────────────────
public class UploadScanViewModel
{
    [Required]
    public int ContractId { get; set; }

    [Required(ErrorMessage = "请选择扫描件文件")]
    public IFormFile ScanFile { get; set; } = null!;
}
