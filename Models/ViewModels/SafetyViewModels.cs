using System.ComponentModel.DataAnnotations;
using EquipmentRental.Models;

namespace EquipmentRental.Models.ViewModels;

// ── 创建安全交底表单 ──────────────────────────────────────────────────────────

public class CreateBriefingViewModel
{
    public int OrderId { get; set; }
    // 页面展示（只读）
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "请选择交底日期")]
    public DateOnly BriefingDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "请填写交底地点")]
    [StringLength(100, ErrorMessage = "地点最多 100 个字符")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "请填写交底内容")]
    public string ContentHtml { get; set; } = string.Empty;

    public IList<ParticipantInputViewModel> Participants { get; set; } =
        [new ParticipantInputViewModel()];

    public IList<IFormFile>? Attachments { get; set; }
}

public class ParticipantInputViewModel
{
    [Required(ErrorMessage = "姓名必填")]
    [StringLength(50, ErrorMessage = "姓名最多 50 个字符")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "工种必填")]
    [StringLength(50, ErrorMessage = "工种最多 50 个字符")]
    public string JobType { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "电话最多 20 个字符")]
    public string? Phone { get; set; }
}

// ── 安全交底详情 ──────────────────────────────────────────────────────────────

public class SafetyDetailViewModel
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public DateOnly BriefingDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public SafetyBriefingStatus Status { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // 参与人（工人，无签署账号）
    public IList<SafetyParticipantViewModel> Workers { get; set; } = [];
    // 附件
    public IList<SafetyAttachmentViewModel> Attachments { get; set; } = [];

    // 签署状态
    public bool SafetyOfficerSigned { get; set; }
    public string? SafetyOfficerSignName { get; set; }
    public DateTime? SafetyOfficerSignAt { get; set; }

    public bool ProjectLeadSigned { get; set; }
    public string? ProjectLeadSignName { get; set; }
    public DateTime? ProjectLeadSignAt { get; set; }

    public bool CanSign { get; set; }
    public bool IsLocked => Status == SafetyBriefingStatus.Completed;

    // Admin 代签记录（不属于 SO/PL 任何一方时使用）
    public string? AdminSignName { get; set; }
    public DateTime? AdminSignAt { get; set; }
}

public class SafetyParticipantViewModel
{
    public string Name { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class SafetyAttachmentViewModel
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
}

// ── 安全交底列表 ──────────────────────────────────────────────────────────────

public class SafetyListViewModel
{
    public IList<SafetyListItemViewModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public SafetyBriefingStatus? StatusFilter { get; set; }
}

// ── 新建交底选单 ──────────────────────────────────────────────────────────────

public class EligibleOrderViewModel
{
    public int OrderId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string EquipmentNo { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public DateOnly ActualStart { get; set; }
    public DateOnly ActualEnd { get; set; }
}

public class SelectOrderViewModel
{
    public IList<EligibleOrderViewModel> Orders { get; set; } = [];
}

public class SafetyListItemViewModel
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly BriefingDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public SafetyBriefingStatus Status { get; set; }
    public string CreatorName { get; set; } = string.Empty;
}
