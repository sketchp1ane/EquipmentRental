using EquipmentRental.Data;
using EquipmentRental.Models;
using EquipmentRental.Models.Entities;
using EquipmentRental.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EquipmentRental.Services;

public class DispatchService(
    AppDbContext db,
    NotificationService notificationService,
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
            EntityType = "Dispatch",
            EntityId   = entityId,
            Detail     = detail,
            OccurredAt = DateTime.UtcNow,
            ClientIp   = ClientIp
        });
        await db.SaveChangesAsync();
    }

    // ── Category helpers ──────────────────────────────────────────────────────

    public async Task<IList<SelectListItem>> GetCategorySelectListAsync()
    {
        var all = await db.EquipmentCategories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var roots    = all.Where(c => c.ParentId == null).ToList();
        var children = all.Where(c => c.ParentId != null).ToList();

        var groups            = roots.ToDictionary(r => r.Id, r => new SelectListGroup { Name = r.Name });
        var rootsWithChildren = children.Select(c => c.ParentId!.Value).ToHashSet();

        var items = new List<SelectListItem>();
        foreach (var root in roots)
        {
            if (!rootsWithChildren.Contains(root.Id))
            {
                items.Add(new SelectListItem { Value = root.Id.ToString(), Text = root.Name });
                continue;
            }

            foreach (var child in children.Where(c => c.ParentId == root.Id)
                                          .OrderBy(c => c.SortOrder).ThenBy(c => c.Name))
            {
                items.Add(new SelectListItem
                {
                    Value = child.Id.ToString(),
                    Text  = child.Name,
                    Group = groups[root.Id]
                });
            }
        }
        return items;
    }

    // ── 4.1 用车申请 ──────────────────────────────────────────────────────────

    public async Task<DispatchRequestFormViewModel> GetRequestFormAsync()
    {
        return new DispatchRequestFormViewModel
        {
            CategoryOptions = await GetCategorySelectListAsync(),
            ExpectedStart   = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            ExpectedEnd     = DateOnly.FromDateTime(DateTime.Today.AddDays(8))
        };
    }

    public async Task<(bool Success, string? Error)> SubmitRequestAsync(
        DispatchRequestFormViewModel form, string requesterId)
    {
        if (form.ExpectedEnd < form.ExpectedStart)
            return (false, "归还日期不能早于开始日期");

        var request = new DispatchRequest
        {
            ProjectName          = form.ProjectName.Trim(),
            ProjectAddress       = form.ProjectAddress.Trim(),
            RequesterId          = requesterId,
            CategoryId           = form.CategoryId,
            Quantity             = form.Quantity,
            ExpectedStart        = form.ExpectedStart,
            ExpectedEnd          = form.ExpectedEnd,
            SpecialRequirements  = string.IsNullOrWhiteSpace(form.SpecialRequirements)
                                     ? null : form.SpecialRequirements.Trim(),
            ContactName          = form.ContactName.Trim(),
            ContactPhone         = form.ContactPhone.Trim(),
            Status               = DispatchRequestStatus.Pending,
            CreatedAt            = DateTime.UtcNow
        };

        db.DispatchRequests.Add(request);
        await db.SaveChangesAsync();

        await WriteOperationLogAsync(requesterId, "SubmitRequest", request.Id.ToString(),
            $"项目：{request.ProjectName}");

        return (true, null);
    }

    // ── 4.2 调度列表 ──────────────────────────────────────────────────────────

    public async Task<DispatchListViewModel> GetRequestListAsync(
        DispatchRequestStatus? statusFilter, int page, int pageSize = 15)
    {
        var query = db.DispatchRequests
            .Include(r => r.Category)
            .Include(r => r.Requester)
            .AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(r => r.Status == statusFilter.Value);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var requestIds = requests.Select(r => r.Id).ToList();
        var orderCounts = await db.DispatchOrders
            .Where(o => requestIds.Contains(o.RequestId))
            .GroupBy(o => o.RequestId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var items = requests.Select(r => new DispatchRequestListItemViewModel
        {
            Id            = r.Id,
            ProjectName   = r.ProjectName,
            CategoryName  = r.Category.Name,
            Quantity      = r.Quantity,
            ExpectedStart = r.ExpectedStart,
            ExpectedEnd   = r.ExpectedEnd,
            RequesterName = r.Requester.RealName,
            ContactPhone  = r.ContactPhone,
            Status        = r.Status,
            CreatedAt     = r.CreatedAt,
            OrderCount    = orderCounts.TryGetValue(r.Id, out var c) ? c : 0
        }).ToList();

        return new DispatchListViewModel
        {
            Items        = items,
            StatusFilter = statusFilter,
            Page         = page,
            TotalPages   = totalPages,
            TotalCount   = totalCount
        };
    }

    // ── 4.2b 调度单列表 ───────────────────────────────────────────────────────

    public async Task<DispatchOrderListViewModel> GetOrderListAsync(
        DispatchOrderStatus? statusFilter,
        string? keyword,
        string? restrictToRequesterId,
        int page,
        int pageSize = 15)
    {
        var query = db.DispatchOrders
            .Include(o => o.Request)
            .Include(o => o.Equipment)
            .Include(o => o.Dispatcher)
            .Include(o => o.Contract)
            .AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(o => o.Status == statusFilter.Value);

        if (!string.IsNullOrWhiteSpace(restrictToRequesterId))
            query = query.Where(o => o.Request.RequesterId == restrictToRequesterId);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(o =>
                o.Request.ProjectName.Contains(kw)
                || o.Equipment.EquipmentNo.Contains(kw)
                || o.Equipment.Name.Contains(kw)
                || (o.Contract != null && o.Contract.ContractNo.Contains(kw)));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = orders.Select(o => new DispatchOrderListItemViewModel
        {
            Id             = o.Id,
            ProjectName    = o.Request.ProjectName,
            EquipmentNo    = o.Equipment.EquipmentNo,
            EquipmentName  = o.Equipment.Name,
            ActualStart    = o.ActualStart,
            ActualEnd      = o.ActualEnd,
            UnitPrice      = o.UnitPrice,
            Status         = o.Status,
            ContractNo     = o.Contract?.ContractNo,
            ContractStatus = o.Contract?.Status,
            DispatcherName = o.Dispatcher.RealName,
            CreatedAt      = o.CreatedAt
        }).ToList();

        return new DispatchOrderListViewModel
        {
            Items        = items,
            StatusFilter = statusFilter,
            Keyword      = keyword,
            Page         = page,
            TotalPages   = totalPages,
            TotalCount   = totalCount
        };
    }

    // ── 4.3 调度排期 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 查询在指定时间段内可用的设备：状态为空闲、资质全部有效、无时间冲突。
    /// </summary>
    public async Task<IList<AvailableEquipmentViewModel>> GetAvailableEquipmentsAsync(
        int categoryId, DateOnly start, DateOnly end)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return await db.Equipments
            .Where(e =>
                e.CategoryId == categoryId
                && e.Status == EquipmentStatus.Idle
                && !db.Qualifications.Any(q =>
                    q.EquipmentId == e.Id && q.ValidTo < today)
                && !db.DispatchOrders.Any(o =>
                    o.EquipmentId == e.Id
                    && o.Status != DispatchOrderStatus.Terminated
                    && o.ActualStart <= end
                    && o.ActualEnd >= start))
            .OrderBy(e => e.EquipmentNo)
            .Select(e => new AvailableEquipmentViewModel
            {
                Id          = e.Id,
                EquipmentNo = e.EquipmentNo,
                Name        = e.Name,
                BrandModel  = e.BrandModel,
                OwnedBy     = e.OwnedBy
            })
            .ToListAsync();
    }

    public async Task<CreateOrderViewModel?> GetCreateOrderViewModelAsync(
        int requestId, DateOnly start, DateOnly end)
    {
        var request = await db.DispatchRequests
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null) return null;

        var available = await GetAvailableEquipmentsAsync(request.CategoryId, start, end);

        return new CreateOrderViewModel
        {
            RequestId           = requestId,
            ProjectName         = request.ProjectName,
            CategoryName        = request.Category.Name,
            CategoryId          = request.CategoryId,
            Quantity            = request.Quantity,
            ExpectedStart       = request.ExpectedStart,
            ExpectedEnd         = request.ExpectedEnd,
            SpecialRequirements = request.SpecialRequirements,
            ContactName         = request.ContactName,
            ContactPhone        = request.ContactPhone,
            ActualStart         = start,
            ActualEnd           = end,
            AvailableEquipments = available
        };
    }

    public async Task<(bool Success, string? Error, int OrderId)> CreateOrderAsync(
        int requestId, int equipmentId, DateOnly actualStart, DateOnly actualEnd,
        decimal unitPrice, decimal deposit, string dispatcherId)
    {
        if (actualEnd < actualStart)
            return (false, "结束日期不能早于开始日期", 0);

        string? error = null;
        DispatchRequest? request = null;
        DispatchOrder? order = null;
        string? contractNo = null;

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();

            request = await db.DispatchRequests
                .Include(r => r.Requester)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) { error = "用车申请不存在"; return; }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var hasExpiredQual = await db.Qualifications.AnyAsync(q =>
                q.EquipmentId == equipmentId && q.ValidTo < today);
            if (hasExpiredQual) { error = "所选设备存在已过期资质，不可调度"; return; }

            var hasConflict = await db.DispatchOrders.AnyAsync(o =>
                o.EquipmentId == equipmentId
                && o.Status != DispatchOrderStatus.Terminated
                && o.ActualStart <= actualEnd
                && o.ActualEnd >= actualStart);
            if (hasConflict) { error = "所选设备在该时间段内已被调度，请选择其他设备或时间"; return; }

            var equipment = await db.Equipments.FindAsync(equipmentId);
            if (equipment == null || equipment.Status != EquipmentStatus.Idle)
            {
                error = "所选设备当前不可用";
                return;
            }

            order = new DispatchOrder
            {
                RequestId    = requestId,
                EquipmentId  = equipmentId,
                DispatcherId = dispatcherId,
                ActualStart  = actualStart,
                ActualEnd    = actualEnd,
                UnitPrice    = unitPrice,
                Deposit      = deposit,
                VerifyCode   = Guid.NewGuid().ToString(),
                Status       = DispatchOrderStatus.Unsigned,
                CreatedAt    = DateTime.UtcNow
            };
            db.DispatchOrders.Add(order);
            await db.SaveChangesAsync();

            contractNo = $"CT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
            db.Contracts.Add(new Contract
            {
                OrderId    = order.Id,
                ContractNo = contractNo,
                Status     = ContractStatus.Draft,
                CreatedAt  = DateTime.UtcNow
            });

            request.Status = DispatchRequestStatus.Scheduled;

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        });

        if (error != null) return (false, error, 0);
        if (order == null || request == null) return (false, "创建失败", 0);

        await notificationService.SendAsync(
            request.Requester.Id,
            $"您的用车申请已排期：{request.ProjectName}",
            $"申请【{request.ProjectName}】已生成调度单，合同编号 {contractNo}，请查看详情。",
            $"/Dispatch/OrderDetails/{order.Id}");

        await WriteOperationLogAsync(dispatcherId, "CreateOrder", order.Id.ToString(),
            $"申请ID:{requestId} 设备ID:{equipmentId} 合同:{contractNo}");

        return (true, null, order.Id);
    }

    public async Task<OrderDetailViewModel?> GetOrderDetailAsync(int orderId)
    {
        var order = await db.DispatchOrders
            .Include(o => o.Request)
            .Include(o => o.Equipment)
            .Include(o => o.Dispatcher)
            .Include(o => o.Contract)
            .Include(o => o.EntryVerification)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return null;

        int days = order.ActualEnd.DayNumber - order.ActualStart.DayNumber + 1;

        return new OrderDetailViewModel
        {
            OrderId        = order.Id,
            RequestId      = order.RequestId,
            ProjectName    = order.Request.ProjectName,
            ProjectAddress = order.Request.ProjectAddress,
            EquipmentNo    = order.Equipment.EquipmentNo,
            EquipmentName  = order.Equipment.Name,
            BrandModel     = order.Equipment.BrandModel,
            ActualStart    = order.ActualStart,
            ActualEnd      = order.ActualEnd,
            UnitPrice      = order.UnitPrice,
            Deposit        = order.Deposit,
            RentalAmount   = order.UnitPrice * days,
            VerifyCode     = order.VerifyCode,
            DispatcherName = order.Dispatcher.RealName,
            ContactName    = order.Request.ContactName,
            ContactPhone   = order.Request.ContactPhone,
            OrderStatus    = order.Status,
            ContractId     = order.Contract?.Id,
            ContractStatus = order.Contract?.Status,
            ContractNo     = order.Contract?.ContractNo,
            CreatedAt      = order.CreatedAt,
            IsVerified     = order.EntryVerification?.IsPass == true
        };
    }

    // ── 4.4 调度日历 ──────────────────────────────────────────────────────────

    public async Task<object> GetCalendarDataAsync(DateOnly start, DateOnly end)
    {
        var orders = await db.DispatchOrders
            .Include(o => o.Equipment)
            .Where(o =>
                o.Status != DispatchOrderStatus.Terminated
                && o.ActualStart <= end
                && o.ActualEnd >= start)
            .ToListAsync();

        var equipments = orders
            .Select(o => new { id = o.EquipmentId, name = o.Equipment.Name, equipmentNo = o.Equipment.EquipmentNo })
            .DistinctBy(x => x.id)
            .OrderBy(x => x.equipmentNo)
            .ToList();

        var slots = orders.Select(o => new
        {
            equipmentId = o.EquipmentId,
            start       = o.ActualStart.ToString("yyyy-MM-dd"),
            end         = o.ActualEnd.ToString("yyyy-MM-dd"),
            orderNo     = o.Id,
            status      = o.Status.ToString()
        }).ToList();

        return new { equipments, slots };
    }

    // ── 4.5 合同详情 ──────────────────────────────────────────────────────────

    private const string ViolationClausesTemplate =
        "1. 乙方应按合同约定按时支付租金，逾期支付每日加收应付款项的 0.5% 违约金。\n" +
        "2. 乙方应妥善保管和使用设备，因使用不当造成设备损坏，乙方须承担全部维修费用。\n" +
        "3. 合同期满，乙方须将设备完好归还甲方，如逾期归还，每日加收日租金 200% 的违约金。\n" +
        "4. 未经甲方书面同意，乙方不得将设备转租或转借第三方。";

    public async Task<ContractDetailViewModel?> GetContractDetailAsync(int contractId)
    {
        var contract = await db.Contracts
            .Include(c => c.Order)
                .ThenInclude(o => o.Request)
                    .ThenInclude(r => r.Requester)
            .Include(c => c.Order)
                .ThenInclude(o => o.Equipment)
            .FirstOrDefaultAsync(c => c.Id == contractId);

        if (contract == null) return null;

        var order = contract.Order;
        var request = order.Request;
        int days = order.ActualEnd.DayNumber - order.ActualStart.DayNumber + 1;

        return new ContractDetailViewModel
        {
            ContractId       = contract.Id,
            OrderId          = order.Id,
            ContractNo       = contract.ContractNo,
            Status           = contract.Status,
            PartyAName       = "某某建机租赁有限公司",
            PartyBName       = request.Requester.RealName,
            PartyBPhone      = request.ContactPhone,
            ProjectName      = request.ProjectName,
            ProjectAddress   = request.ProjectAddress,
            EquipmentNo      = order.Equipment.EquipmentNo,
            EquipmentName    = order.Equipment.Name,
            BrandModel       = order.Equipment.BrandModel,
            OwnedBy          = order.Equipment.OwnedBy,
            RentalStart      = order.ActualStart,
            RentalEnd        = order.ActualEnd,
            RentalDays       = days,
            UnitPrice        = order.UnitPrice,
            RentalAmount     = order.UnitPrice * days,
            Deposit          = order.Deposit,
            ViolationClauses = ViolationClausesTemplate,
            CreatedAt        = contract.CreatedAt,
            ScanPath         = contract.ScanPath
        };
    }

    // ── 4.6 合同 PDF 导出 ─────────────────────────────────────────────────────

    public async Task<byte[]?> ExportContractPdfAsync(int contractId)
    {
        var vm = await GetContractDetailAsync(contractId);
        if (vm == null) return null;

        var doc = new ContractDocument(vm);
        return doc.GeneratePdf();
    }

    // ── 4.7 上传扫描件 ────────────────────────────────────────────────────────

    public async Task<(bool Success, string? Error)> UploadScanAsync(
        int contractId, IFormFile file, string actorId, FileService fileService)
    {
        var contract = await db.Contracts
            .Include(c => c.Order)
                .ThenInclude(o => o.Equipment)
            .FirstOrDefaultAsync(c => c.Id == contractId);

        if (contract == null)
            return (false, "合同不存在");

        if (contract.Status == ContractStatus.Signed)
            return (false, "合同已签署，不可重复上传");

        var filePath = await fileService.SaveFileAsync(file, "ContractScans");
        if (filePath == null)
            return (false, "文件类型不支持，请上传 JPG/PNG/PDF 格式");

        contract.ScanPath = filePath;
        contract.Status   = ContractStatus.Signed;
        contract.Order.Status = DispatchOrderStatus.Signed;
        contract.Order.Equipment.Status = EquipmentStatus.InUse;

        await db.SaveChangesAsync();

        await WriteOperationLogAsync(actorId, "UploadContractScan", contractId.ToString(),
            $"文件：{filePath}");

        return (true, null);
    }

    // ── QuestPDF 合同文档 ─────────────────────────────────────────────────────

    private sealed class ContractDocument(ContractDetailViewModel vm) : IDocument
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
                    col.Item().AlignCenter().Text("设备租赁合同")
                        .FontSize(18).Bold();
                    col.Item().AlignCenter().Text($"合同编号：{vm.ContractNo}")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                    // 甲乙双方
                    col.Item().Text("一、甲乙双方").Bold().FontSize(11);
                    col.Item().PaddingTop(4).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(5); });
                        AddRow(t, "甲方（出租方）", vm.PartyAName);
                        AddRow(t, "乙方（承租方）", vm.PartyBName);
                        AddRow(t, "乙方联系电话", vm.PartyBPhone);
                    });

                    col.Item().PaddingTop(10).Text("二、项目信息").Bold().FontSize(11);
                    col.Item().PaddingTop(4).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(5); });
                        AddRow(t, "项目名称", vm.ProjectName);
                        AddRow(t, "项目地址", vm.ProjectAddress);
                    });

                    col.Item().PaddingTop(10).Text("三、设备信息").Bold().FontSize(11);
                    col.Item().PaddingTop(4).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(5); });
                        AddRow(t, "设备编号", vm.EquipmentNo);
                        AddRow(t, "设备名称", vm.EquipmentName);
                        AddRow(t, "品牌型号", vm.BrandModel);
                        AddRow(t, "所属单位", vm.OwnedBy);
                    });

                    col.Item().PaddingTop(10).Text("四、租期与费用").Bold().FontSize(11);
                    col.Item().PaddingTop(4).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(5); });
                        AddRow(t, "租赁开始", vm.RentalStart.ToString("yyyy-MM-dd"));
                        AddRow(t, "租赁结束", vm.RentalEnd.ToString("yyyy-MM-dd"));
                        AddRow(t, "租赁天数", $"{vm.RentalDays} 天");
                        AddRow(t, "日租金（元/天）", $"¥ {vm.UnitPrice:N2}");
                        AddRow(t, "租赁金额（元）", $"¥ {vm.RentalAmount:N2}");
                        AddRow(t, "押金（元）", $"¥ {vm.Deposit:N2}");
                    });

                    col.Item().PaddingTop(10).Text("五、违约条款").Bold().FontSize(11);
                    col.Item().PaddingTop(4).Text(vm.ViolationClauses).FontSize(9);

                    col.Item().PaddingTop(20).Text("六、签署").Bold().FontSize(11);
                    col.Item().PaddingTop(8).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                        t.Cell().Text("签署日期：____________");
                        t.Cell().Text("甲方签章：____________");
                        t.Cell().Text("乙方签章：____________");
                    });

                    col.Item().PaddingTop(6).AlignRight()
                        .Text($"生成日期：{vm.CreatedAt.ToLocalTime():yyyy-MM-dd}")
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
