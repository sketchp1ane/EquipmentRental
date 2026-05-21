# 附录 A 核心代码节选

> 说明：以下代码按“完整业务片段”节选，而不是零散函数摘录。每段都尽量保留上下文，便于在论文附录中展示系统的核心实现逻辑。正式排版时可保留 4-6 段。

## A.1 系统启动、安全配置与业务服务注册

选取理由：该片段展示系统启动阶段的核心配置，包括 EF Core 数据库连接、Identity 登录安全策略、BCrypt 密码哈希替换、全局 CSRF 防护、业务服务依赖注入，以及数据库迁移和种子数据初始化。

源文件：`Program.cs`

```csharp
var seedOnly = args.Contains("--seed-only", StringComparer.OrdinalIgnoreCase);

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(3)
    ));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.AllowedForNewUsers = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, BCryptPasswordHasher>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
})
.AddRazorRuntimeCompilation();

builder.Services.AddScoped<EquipmentService>();
builder.Services.AddScoped<QualificationService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<DispatchService>();
builder.Services.AddScoped<VerificationService>();
builder.Services.AddScoped<SafetyService>();
builder.Services.AddScoped<InspectionService>();
builder.Services.AddScoped<FaultService>();
builder.Services.AddScoped<ReturnService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<Ganss.Xss.HtmlSanitizer>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbInitializer.SeedAsync(scope.ServiceProvider);
}

if (seedOnly)
{
    Console.WriteLine("Database migrated and seeded.");
    return;
}

await app.RunAsync();
```

## A.2 调度排期、调度单创建与合同签署状态推进

选取理由：该片段覆盖“线上调度”模块的主要业务逻辑：筛选可用设备、校验资质与时间冲突、事务化创建调度单和合同草稿、生成核验码，以及合同扫描件上传后推动合同、调度单和设备状态同步变化。

源文件：`Services/DispatchService.cs`

```csharp
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
```

## A.3 进场核验核心流程

选取理由：该片段展示“进场核验”完整流程，包括核验码查询、订单状态校验、有效期校验、防重复核验、设备状态校验、资质有效期校验、核验记录写入、调度单状态推进和通知发送。

源文件：`Services/VerificationService.cs`

```csharp
public async Task<(bool Success, bool IsPass, string? FailReason, int? VerificationId)>
    PerformVerifyAsync(string verifyCode, string verifierId)
{
    var today = DateOnly.FromDateTime(DateTime.Today);

    var order = await db.DispatchOrders
        .Include(o => o.Equipment)
        .Include(o => o.Request)
        .Include(o => o.Dispatcher)
        .FirstOrDefaultAsync(o => o.VerifyCode == verifyCode);

    if (order == null)
        return (false, false, "核验码无效", null);

    if (order.Status == DispatchOrderStatus.InProgress
        || order.Status == DispatchOrderStatus.Terminated)
    {
        var existingPass = await db.EntryVerifications
            .FirstOrDefaultAsync(ev => ev.OrderId == order.Id && ev.IsPass);
        if (existingPass != null)
            return (true, true, null, existingPass.Id);
    }

    if (order.Status != DispatchOrderStatus.Signed)
        return await RecordFailAsync(order.Id, verifierId,
            "调度单尚未签署，无法核验");

    if (today > order.ActualStart.AddDays(3))
        return await RecordFailAsync(order.Id, verifierId,
            "核验码已过期（有效期为租赁开始日期后 3 天）");

    var existingPass2 = await db.EntryVerifications
        .FirstOrDefaultAsync(ev => ev.OrderId == order.Id && ev.IsPass);
    if (existingPass2 != null)
        return (true, true, null, existingPass2.Id);

    if (order.Equipment.Status == EquipmentStatus.Idle)
        order.Equipment.Status = EquipmentStatus.InUse;
    else if (order.Equipment.Status != EquipmentStatus.InUse)
        return await RecordFailAsync(order.Id, verifierId,
            "设备当前状态异常，无法核验（设备需处于出租中状态）");

    var expiredCerts = await db.Qualifications
        .Where(q => q.EquipmentId == order.EquipmentId && q.ValidTo < today)
        .ToListAsync();

    if (expiredCerts.Count > 0)
    {
        var names = expiredCerts.Select(q => q.Type.ToString()).Distinct();
        return await RecordFailAsync(order.Id, verifierId,
            $"以下设备证件已过期：{string.Join("、", names)}");
    }

    var verification = await db.EntryVerifications
        .FirstOrDefaultAsync(ev => ev.OrderId == order.Id);
    if (verification == null)
    {
        verification = new EntryVerification { OrderId = order.Id };
        db.EntryVerifications.Add(verification);
    }
    verification.VerifierId = verifierId;
    verification.VerifiedAt = DateTime.UtcNow;
    verification.IsPass     = true;
    verification.FailReason = null;
    order.Status = DispatchOrderStatus.InProgress;
    await db.SaveChangesAsync();

    await WriteOperationLogAsync(verifierId, "Verify", verification.Id.ToString(),
        $"{{\"OrderId\":{order.Id},\"IsPass\":true}}");

    await notificationService.SendAsync(
        order.DispatcherId,
        "进场核验通过",
        $"调度单 #{order.Id}（{order.Request.ProjectName}）已完成进场核验。",
        $"/Verification/Details/{verification.Id}");

    return (true, true, null, verification.Id);
}

private async Task<(bool Success, bool IsPass, string? FailReason, int? VerificationId)>
    RecordFailAsync(int? orderId, string verifierId, string reason)
{
    if (orderId.HasValue)
    {
        var verification = await db.EntryVerifications
            .FirstOrDefaultAsync(ev => ev.OrderId == orderId.Value);
        if (verification == null)
        {
            verification = new EntryVerification { OrderId = orderId.Value };
            db.EntryVerifications.Add(verification);
        }
        verification.VerifierId = verifierId;
        verification.VerifiedAt = DateTime.UtcNow;
        verification.IsPass     = false;
        verification.FailReason = reason;
        await db.SaveChangesAsync();

        await WriteOperationLogAsync(verifierId, "VerifyFail", verification.Id.ToString(),
            $"{{\"OrderId\":{orderId.Value},\"IsPass\":false,\"Reason\":\"{reason}\"}}");

        return (true, false, reason, verification.Id);
    }

    return (false, false, reason, null);
}
```

## A.4 安全交底创建、富文本过滤与双签确认

选取理由：该片段展示安全交底模块的核心实现：只有已进场核验通过的调度单才能创建交底；交底内容在入库前进行 HTML 净化；附件通过统一文件服务保存；安全员和项目负责人完成签署后，交底状态自动变更为已完成。

源文件：`Services/SafetyService.cs`

```csharp
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

public async Task<(bool Success, string? Error, int Id)> CreateBriefingAsync(
    CreateBriefingViewModel vm, string creatorId)
{
    if (vm.Attachments?.Count > 10)
        return (false, "附件最多 10 个", 0);

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
    await db.SaveChangesAsync();

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

    await notificationService.SendAsync(
        order.Request.RequesterId,
        "安全交底待签署",
        $"调度单 #{order.Id}（{order.Request.ProjectName}）的安全交底记录已创建，请前往签署确认。",
        $"/Safety/Details/{briefing.Id}");

    return (true, null, briefing.Id);
}

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
```

## A.5 使用监管、故障闭环与退场评价

选取理由：该片段覆盖租赁使用阶段和退场阶段的关键闭环：故障上报后设备进入维修中，故障关闭后设备按订单状态恢复；退场评价校验扣款边界，计算押金退还金额，并将退场申请、调度单和设备状态同步更新。

源文件：`Services/FaultService.cs`、`Services/ReturnService.cs`

```csharp
public async Task<(bool Success, string? Error, int Id)> CreateFaultAsync(
    CreateFaultViewModel vm, string reporterId)
{
    if (vm.Images?.Count > 10)
        return (false, "故障照片最多 10 张", 0);

    var order = await db.DispatchOrders
        .Include(o => o.Equipment)
        .Include(o => o.Request)
        .FirstOrDefaultAsync(o => o.Id == vm.OrderId);

    if (order == null || order.Status != DispatchOrderStatus.InProgress)
        return (false, "调度单不存在或状态不符", 0);

    var report = new FaultReport
    {
        EquipmentId = order.EquipmentId,
        OrderId     = vm.OrderId,
        ReporterId  = reporterId,
        Description = vm.Description,
        Severity    = vm.Severity,
        ReportedAt  = DateTime.UtcNow,
        Status      = FaultStatus.Pending
    };
    db.FaultReports.Add(report);
    await db.SaveChangesAsync();

    if (vm.Images != null)
    {
        foreach (var file in vm.Images)
        {
            var path = await fileService.SaveFileAsync(file, "Fault");
            db.FaultImages.Add(new FaultImage
            {
                FaultReportId = report.Id,
                FilePath      = path,
                UploadedAt    = DateTime.UtcNow
            });
        }
    }

    order.Equipment.Status = EquipmentStatus.Maintenance;

    await db.SaveChangesAsync();

    await WriteOperationLogAsync(reporterId, "Create", report.Id.ToString(),
        $"{{\"OrderId\":{vm.OrderId},\"Severity\":\"{vm.Severity}\"}}");

    var admins = await userManager.GetUsersInRoleAsync(Roles.DeviceAdmin);
    foreach (var admin in admins)
    {
        await notificationService.SendAsync(
            admin.Id,
            "新故障上报",
            $"设备 {order.Equipment.EquipmentNo}（{order.Request.ProjectName}）上报故障，请前往处理。",
            $"/Fault/Details/{report.Id}");
    }

    return (true, null, report.Id);
}

public async Task<(bool Success, string? Error, string RestoredStatusText)> CloseFaultAsync(
    CloseFaultViewModel vm, string operatorId)
{
    var report = await db.FaultReports
        .Include(fr => fr.Equipment)
        .Include(fr => fr.Order)
        .Include(fr => fr.Reporter)
        .FirstOrDefaultAsync(fr => fr.Id == vm.Id);

    if (report == null)
        return (false, "故障工单不存在", string.Empty);

    if (report.Status != FaultStatus.InProgress)
        return (false, "故障工单当前状态不可关闭", string.Empty);

    report.Status     = FaultStatus.Closed;
    report.Resolution = vm.Resolution;
    report.RepairCost = vm.RepairCost;
    report.ClosedById = operatorId;
    report.ClosedAt   = DateTime.UtcNow;

    report.Equipment.Status = report.Order.Status == DispatchOrderStatus.InProgress
        ? EquipmentStatus.InUse
        : EquipmentStatus.Idle;

    var restoredStatusText = GetEquipmentStatusText(report.Equipment.Status);

    await db.SaveChangesAsync();

    await WriteOperationLogAsync(operatorId, "Close", vm.Id.ToString(),
        $"{{\"Resolution\":\"{vm.Resolution}\",\"RepairCost\":{vm.RepairCost}}}");

    await notificationService.SendAsync(
        report.ReporterId,
        "故障工单已关闭",
        $"您上报的故障（工单 #{report.Id}）已处理完成，设备已恢复为{restoredStatusText}状态。",
        $"/Fault/Details/{report.Id}");

    return (true, null, restoredStatusText);
}

public async Task<(bool Success, string? Error)> CreateEvaluationAsync(
    EvaluateReturnViewModel vm, string evaluatorId)
{
    var app = await db.ReturnApplications
        .Include(r => r.Order)
            .ThenInclude(o => o.Equipment)
        .Include(r => r.Evaluation)
        .FirstOrDefaultAsync(r => r.Id == vm.ReturnAppId);

    if (app == null)
        return (false, "退场申请不存在");

    if (app.Status != ReturnApplicationStatus.PendingEvaluation)
        return (false, "退场申请当前状态不可填写评价");

    if (app.Evaluation != null)
        return (false, "该退场申请已填写过评价");

    if (vm.NewEquipmentStatus is not (EquipmentStatus.Idle or EquipmentStatus.Maintenance or EquipmentStatus.Scrapped))
        return (false, "请选择有效的退场后设备状态");

    if (vm.Deduction < 0)
        return (false, "扣款金额不能为负数");

    if (vm.Deduction > app.Order.Deposit)
        return (false, $"扣款金额不能超过押金金额 ¥{app.Order.Deposit:N2}");

    decimal refundAmount = app.Order.Deposit - vm.Deduction;

    var evaluation = new ReturnEvaluation
    {
        ReturnAppId     = vm.ReturnAppId,
        EvaluatorId     = evaluatorId,
        AppearanceScore = vm.AppearanceScore,
        FunctionScore   = vm.FunctionScore,
        DamageDesc      = vm.DamageDesc,
        Deduction       = vm.Deduction,
        RefundAmount    = refundAmount,
        Remark          = vm.Remark,
        EvaluatedAt     = DateTime.UtcNow
    };
    db.ReturnEvaluations.Add(evaluation);

    app.Status             = ReturnApplicationStatus.Complete;
    app.Order.Status       = DispatchOrderStatus.Complete;
    app.Order.Equipment.Status = vm.NewEquipmentStatus;

    await db.SaveChangesAsync();

    await WriteOperationLogAsync(evaluatorId, "Evaluate", app.Id.ToString(),
        $"{{\"ReturnAppId\":{vm.ReturnAppId},\"RefundAmount\":{refundAmount},\"NewStatus\":\"{vm.NewEquipmentStatus}\"}}");

    await notificationService.SendAsync(
        app.ApplicantId,
        "退场评价已完成",
        $"您提交的退场申请（调度单 #{app.OrderId}，设备 {app.Order.Equipment.EquipmentNo}）已完成评价，押金退还金额：¥{refundAmount:N2}。",
        $"/Return/Details/{app.Id}");

    return (true, null);
}
```
