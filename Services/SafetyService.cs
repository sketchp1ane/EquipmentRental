using EquipmentRental.Constants;
using EquipmentRental.Data;
using EquipmentRental.Models;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using Ganss.Xss;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EquipmentRental.Services;

public class SafetyService(
    AppDbContext db,
    FileService fileService,
    HtmlSanitizer sanitizer,
    NotificationService notificationService,
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor)
{
    private string? ClientIp =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private async Task WriteOperationLogAsync(
        string actorId, string action, string entityId, string? detail = null)
    {
        db.OperationLogs.Add(new OperationLog
        {
            UserId     = actorId,
            Action     = action,
            EntityType = "SafetyBriefing",
            EntityId   = entityId,
            Detail     = detail,
            OccurredAt = DateTime.UtcNow,
            ClientIp   = ClientIp
        });
        await db.SaveChangesAsync();
    }

    // ── 获取创建表单数据 ──────────────────────────────────────────────────────

    public async Task<CreateBriefingViewModel?> GetCreateFormAsync(int orderId)
    {
        var order = await db.DispatchOrders
            .Include(o => o.Request)
            .Include(o => o.Equipment)
            .Include(o => o.EntryVerification)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null
            || order.Status != DispatchOrderStatus.InProgress
            || order.EntryVerification?.IsPass != true)
            return null;

        return new CreateBriefingViewModel
        {
            OrderId      = orderId,
            ProjectName  = order.Request.ProjectName,
            EquipmentNo  = order.Equipment.EquipmentNo,
            BriefingDate = DateOnly.FromDateTime(DateTime.Today)
        };
    }

    // ── 创建安全交底 ──────────────────────────────────────────────────────────

    public async Task<(bool Success, string? Error, int Id)> CreateBriefingAsync(
        CreateBriefingViewModel vm, string creatorId)
    {
        if (vm.Attachments?.Count > 10)
            return (false, "附件最多 10 个", 0);

        // 加载项目负责人 ID
        var order = await db.DispatchOrders
            .Include(o => o.Request)
            .FirstOrDefaultAsync(o => o.Id == vm.OrderId);
        if (order == null)
            return (false, "调度单不存在", 0);

        var sanitizedHtml = sanitizer.Sanitize(vm.ContentHtml);

        var briefing = new SafetyBriefing
        {
            OrderId      = vm.OrderId,
            CreatorId    = creatorId,
            BriefingDate = vm.BriefingDate,
            Location     = vm.Location,
            ContentHtml  = sanitizedHtml,
            Status       = SafetyBriefingStatus.Draft,
            CreatedAt    = DateTime.UtcNow
        };
        db.SafetyBriefings.Add(briefing);
        await db.SaveChangesAsync();  // get Id

        // 参与人（工人）
        foreach (var p in vm.Participants.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
        {
            db.BriefingParticipants.Add(new BriefingParticipant
            {
                BriefingId = briefing.Id,
                Name       = p.Name,
                JobType    = p.JobType,
                Phone      = p.Phone
            });
        }

        // 附件
        if (vm.Attachments != null)
        {
            foreach (var file in vm.Attachments)
            {
                var path = await fileService.SaveFileAsync(file, "Safety");
                db.BriefingAttachments.Add(new BriefingAttachment
                {
                    BriefingId   = briefing.Id,
                    FilePath     = path,
                    OriginalName = file.FileName
                });
            }
        }

        await db.SaveChangesAsync();

        await WriteOperationLogAsync(creatorId, "Create", briefing.Id.ToString(),
            $"{{\"OrderId\":{vm.OrderId},\"Location\":\"{vm.Location}\"}}");

        // 通知项目负责人
        await notificationService.SendAsync(
            order.Request.RequesterId,
            "安全交底待签署",
            $"调度单 #{order.Id}（{order.Request.ProjectName}）的安全交底记录已创建，请前往签署确认。",
            $"/Safety/Details/{briefing.Id}");

        return (true, null, briefing.Id);
    }

    // ── 详情 ──────────────────────────────────────────────────────────────────

    public async Task<SafetyDetailViewModel?> GetDetailAsync(int id, string currentUserId)
    {
        var briefing = await db.SafetyBriefings
            .Include(sb => sb.Order)
                .ThenInclude(o => o.Request)
            .Include(sb => sb.Order)
                .ThenInclude(o => o.Equipment)
            .Include(sb => sb.Creator)
            .Include(sb => sb.Participants)
                .ThenInclude(p => p.SignedBy)
            .Include(sb => sb.Attachments)
            .FirstOrDefaultAsync(sb => sb.Id == id);

        if (briefing == null) return null;

        var projectLeadId = briefing.Order.Request.RequesterId;

        // 安全员签署行（CreatorId 的那条）
        var soSign = briefing.Participants
            .FirstOrDefault(p => p.SignedById == briefing.CreatorId);
        // 项目负责人签署行
        var plSign = briefing.Participants
            .FirstOrDefault(p => p.SignedById == projectLeadId);

        bool alreadySigned = briefing.Participants
            .Any(p => p.SignedById == currentUserId);

        var currentUser = await userManager.FindByIdAsync(currentUserId);
        bool isCurrentUserAdmin = currentUser != null
            && await userManager.IsInRoleAsync(currentUser, Roles.Admin);

        bool isEligibleSigner = currentUserId == briefing.CreatorId
                                || currentUserId == projectLeadId
                                || isCurrentUserAdmin;

        // Admin 代签记录（JobType == "系统管理员"，且 SignedById 不是 SO/PL）
        var adminSign = briefing.Participants
            .FirstOrDefault(p => p.SignedById != null
                && p.SignedById != briefing.CreatorId
                && p.SignedById != projectLeadId
                && p.JobType == "系统管理员");

        // 工人参与人（没有签署账号的，即 SignedById == null）
        var workers = briefing.Participants
            .Where(p => p.SignedById == null)
            .Select(p => new SafetyParticipantViewModel
            {
                Name    = p.Name,
                JobType = p.JobType,
                Phone   = p.Phone
            })
            .ToList();

        return new SafetyDetailViewModel
        {
            Id           = briefing.Id,
            OrderId      = briefing.OrderId,
            ProjectName  = briefing.Order.Request.ProjectName,
            EquipmentNo  = briefing.Order.Equipment.EquipmentNo,
            EquipmentName = briefing.Order.Equipment.Name,
            BriefingDate = briefing.BriefingDate,
            Location     = briefing.Location,
            ContentHtml  = briefing.ContentHtml,
            Status       = briefing.Status,
            CreatorName  = briefing.Creator.RealName,
            CreatedAt    = briefing.CreatedAt,
            Workers      = workers,
            Attachments  = briefing.Attachments.Select(a => new SafetyAttachmentViewModel
            {
                Id           = a.Id,
                FilePath     = a.FilePath,
                OriginalName = a.OriginalName
            }).ToList(),
            SafetyOfficerSigned  = soSign != null,
            SafetyOfficerSignName = soSign?.SignedBy?.RealName,
            SafetyOfficerSignAt  = soSign?.SignedAt,
            ProjectLeadSigned    = plSign != null,
            ProjectLeadSignName  = plSign?.SignedBy?.RealName,
            ProjectLeadSignAt    = plSign?.SignedAt,
            AdminSignName        = adminSign?.SignedBy?.RealName,
            AdminSignAt          = adminSign?.SignedAt,
            CanSign = briefing.Status == SafetyBriefingStatus.Draft
                      && isEligibleSigner
                      && !alreadySigned
        };
    }

    // ── 签署确认 ──────────────────────────────────────────────────────────────

    public async Task<(bool Success, string? Error)> SignAsync(int briefingId, string userId)
    {
        var briefing = await db.SafetyBriefings
            .Include(sb => sb.Order)
                .ThenInclude(o => o.Request)
            .Include(sb => sb.Participants)
            .FirstOrDefaultAsync(sb => sb.Id == briefingId);

        if (briefing == null)
            return (false, "安全交底记录不存在");

        if (briefing.Status == SafetyBriefingStatus.Completed)
            return (false, "安全交底已完成，不可重复签署");

        var projectLeadId = briefing.Order.Request.RequesterId;

        var user = await userManager.FindByIdAsync(userId);
        bool isAdmin = user != null && await userManager.IsInRoleAsync(user, Roles.Admin);

        if (!isAdmin && userId != briefing.CreatorId && userId != projectLeadId)
            return (false, "您无权签署此安全交底");

        if (briefing.Participants.Any(p => p.SignedById == userId))
            return (false, "您已签署此安全交底");

        var jobType = isAdmin ? "系统管理员" : (userId == briefing.CreatorId ? "安全员" : "项目负责人");

        db.BriefingParticipants.Add(new BriefingParticipant
        {
            BriefingId = briefingId,
            Name       = user?.RealName ?? "未知",
            JobType    = jobType,
            SignedById = userId,
            SignedAt   = DateTime.UtcNow,
            ClientIp   = ClientIp
        });
        await db.SaveChangesAsync();

        // 检查是否两方均已签署
        var allParticipants = await db.BriefingParticipants
            .Where(p => p.BriefingId == briefingId)
            .ToListAsync();

        bool soSigned = allParticipants.Any(p => p.SignedById == briefing.CreatorId);
        bool plSigned = allParticipants.Any(p => p.SignedById == projectLeadId);

        if (isAdmin || (soSigned && plSigned))
        {
            briefing.Status = SafetyBriefingStatus.Completed;
            await db.SaveChangesAsync();
        }

        await WriteOperationLogAsync(userId, "Sign", briefingId.ToString(),
            $"{{\"JobType\":\"{jobType}\"}}");

        return (true, null);
    }

    // ── 列表 ──────────────────────────────────────────────────────────────────

    public async Task<SafetyListViewModel> GetListAsync(
        SafetyBriefingStatus? status = null, int page = 1, int pageSize = 15)
    {
        var query = db.SafetyBriefings
            .Include(sb => sb.Order)
                .ThenInclude(o => o.Request)
            .Include(sb => sb.Creator)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(sb => sb.Status == status.Value);

        query = query.OrderByDescending(sb => sb.CreatedAt);

        int total = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(total / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sb => new SafetyListItemViewModel
            {
                Id           = sb.Id,
                OrderId      = sb.OrderId,
                ProjectName  = sb.Order.Request.ProjectName,
                BriefingDate = sb.BriefingDate,
                Location     = sb.Location,
                Status       = sb.Status,
                CreatorName  = sb.Creator.RealName
            })
            .ToListAsync();

        return new SafetyListViewModel
        {
            Items        = items,
            TotalCount   = total,
            Page         = page,
            PageSize     = pageSize,
            StatusFilter = status
        };
    }

    public async Task<SelectOrderViewModel> GetEligibleOrdersAsync()
    {
        var orders = await db.DispatchOrders
            .Include(o => o.Equipment)
            .Include(o => o.Request)
            .Include(o => o.EntryVerification)
            .Where(o => o.Status == DispatchOrderStatus.InProgress
                        && o.EntryVerification != null
                        && o.EntryVerification.IsPass)
            .OrderByDescending(o => o.ActualStart)
            .Select(o => new EligibleOrderViewModel
            {
                OrderId      = o.Id,
                ProjectName  = o.Request.ProjectName,
                EquipmentNo  = o.Equipment.EquipmentNo,
                EquipmentName = o.Equipment.Name,
                ActualStart  = o.ActualStart,
                ActualEnd    = o.ActualEnd
            })
            .ToListAsync();

        return new SelectOrderViewModel { Orders = orders };
    }

    // ── PDF 导出 ──────────────────────────────────────────────────────────────

    public async Task<byte[]?> ExportPdfAsync(int id)
    {
        var briefing = await db.SafetyBriefings
            .Include(sb => sb.Order)
                .ThenInclude(o => o.Request)
            .Include(sb => sb.Order)
                .ThenInclude(o => o.Equipment)
            .Include(sb => sb.Creator)
            .Include(sb => sb.Participants)
                .ThenInclude(p => p.SignedBy)
            .Include(sb => sb.Attachments)
            .FirstOrDefaultAsync(sb => sb.Id == id);

        if (briefing == null) return null;

        return new BriefingDocument(briefing).GeneratePdf();
    }

    // ── QuestPDF 文档 ─────────────────────────────────────────────────────────

    private sealed class BriefingDocument(SafetyBriefing b) : IDocument
    {
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Content().Column(col =>
                {
                    // 标题
                    col.Item().AlignCenter().Text("安全交底记录")
                        .FontSize(18).Bold();
                    col.Item().AlignCenter().Text($"调度单 #{b.OrderId}")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    // 基本信息
                    col.Item().Text("一、基本信息").Bold().FontSize(11);
                    col.Item().PaddingTop(4).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(5); });
                        AddRow(t, "项目名称", b.Order.Request.ProjectName);
                        AddRow(t, "设备编号", b.Order.Equipment.EquipmentNo);
                        AddRow(t, "交底日期", b.BriefingDate.ToString("yyyy-MM-dd"));
                        AddRow(t, "交底地点", b.Location);
                        AddRow(t, "安全员", b.Creator.RealName);
                    });

                    // 交底内容
                    col.Item().PaddingTop(10).Text("二、交底内容").Bold().FontSize(11);
                    var plainText = System.Text.RegularExpressions.Regex
                        .Replace(b.ContentHtml, "<[^>]+>", "").Trim();
                    col.Item().PaddingTop(4).Text(plainText).FontSize(9);

                    // 参与人
                    var workers = b.Participants
                        .Where(p => p.SignedById == null).ToList();
                    if (workers.Count > 0)
                    {
                        col.Item().PaddingTop(10).Text("三、参与人员").Bold().FontSize(11);
                        col.Item().PaddingTop(4).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });
                            t.Cell().Padding(4).Background(Colors.Grey.Lighten3).Text("姓名").Bold().FontSize(9);
                            t.Cell().Padding(4).Background(Colors.Grey.Lighten3).Text("工种").Bold().FontSize(9);
                            t.Cell().Padding(4).Background(Colors.Grey.Lighten3).Text("电话").Bold().FontSize(9);
                            foreach (var w in workers)
                            {
                                t.Cell().Padding(4).Text(w.Name).FontSize(9);
                                t.Cell().Padding(4).Text(w.JobType).FontSize(9);
                                t.Cell().Padding(4).Text(w.Phone ?? "—").FontSize(9);
                            }
                        });
                    }

                    // 签署信息
                    col.Item().PaddingTop(10).Text("四、电子签署").Bold().FontSize(11);
                    col.Item().PaddingTop(4).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(3); c.RelativeColumn(3); });
                        t.Cell().Padding(4).Background(Colors.Grey.Lighten3).Text("角色").Bold().FontSize(9);
                        t.Cell().Padding(4).Background(Colors.Grey.Lighten3).Text("签署人").Bold().FontSize(9);
                        t.Cell().Padding(4).Background(Colors.Grey.Lighten3).Text("签署时间").Bold().FontSize(9);

                        var soSign = b.Participants.FirstOrDefault(p => p.SignedById == b.CreatorId);
                        t.Cell().Padding(4).Text("安全员").FontSize(9);
                        t.Cell().Padding(4).Text(soSign?.SignedBy?.RealName ?? "待签署").FontSize(9);
                        t.Cell().Padding(4).Text(soSign?.SignedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "—").FontSize(9);

                        var plSign = b.Participants
                            .FirstOrDefault(p => p.SignedById != null && p.SignedById != b.CreatorId);
                        t.Cell().Padding(4).Text("项目负责人").FontSize(9);
                        t.Cell().Padding(4).Text(plSign?.SignedBy?.RealName ?? "待签署").FontSize(9);
                        t.Cell().Padding(4).Text(plSign?.SignedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "—").FontSize(9);
                    });

                    col.Item().PaddingTop(6).AlignRight()
                        .Text($"生成日期：{DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }

        private static void AddRow(TableDescriptor t, string label, string value)
        {
            t.Cell().Padding(4).Background(Colors.Grey.Lighten4).Text(label).Bold().FontSize(9);
            t.Cell().Padding(4).Text(value).FontSize(9);
        }
    }
}
