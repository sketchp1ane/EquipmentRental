using EquipmentRental.Constants;
using EquipmentRental.Models;
using EquipmentRental.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EquipmentRental.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db          = services.GetRequiredService<AppDbContext>();

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole(roleName));
        }

        const string adminEmail = "admin@equiprental.com";
        const string adminPassword = "Admin@123456";

        var admin = await userManager.FindByNameAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                RealName = "系统管理员",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true,
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
                throw new Exception(
                    $"创建管理员账号失败：{string.Join(", ", result.Errors.Select(e => e.Description))}");

            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
        else if (admin.PasswordHash != null && !admin.PasswordHash.StartsWith("$2"))
        {
            // Migrate from PBKDF2 to BCrypt on first startup after hasher swap
            await userManager.RemovePasswordAsync(admin);
            await userManager.AddPasswordAsync(admin, adminPassword);
        }

        await SeedCategoriesAsync(db);
        await SeedDemoDataAsync(db, userManager);
    }

    // ── 预置设备分类 ──────────────────────────────────────────────────────────

    private static async Task SeedCategoriesAsync(AppDbContext db)
    {
        if (await db.EquipmentCategories.AnyAsync()) return;

        // 一级分类
        var roots = new[]
        {
            new EquipmentCategory { Name = "起重机械",   Level = 1, SortOrder = 1 },
            new EquipmentCategory { Name = "土石方机械", Level = 1, SortOrder = 2 },
            new EquipmentCategory { Name = "混凝土机械", Level = 1, SortOrder = 3 },
            new EquipmentCategory { Name = "桩工机械",   Level = 1, SortOrder = 4 },
            new EquipmentCategory { Name = "高空作业机械",Level = 1, SortOrder = 5 },
            new EquipmentCategory { Name = "钢筋加工机械",Level = 1, SortOrder = 6 },
            new EquipmentCategory { Name = "焊接设备",   Level = 1, SortOrder = 7 },
            new EquipmentCategory { Name = "动力与电气",  Level = 1, SortOrder = 8 },
        };
        db.EquipmentCategories.AddRange(roots);
        await db.SaveChangesAsync();

        // 二级分类（按父级名称关联，避免硬编码 ID）
        var sub = new[]
        {
            // 起重机械
            ("起重机械", "塔式起重机",     1),
            ("起重机械", "汽车起重机",     2),
            ("起重机械", "履带起重机",     3),
            ("起重机械", "施工升降机",     4),
            ("起重机械", "物料提升机",     5),

            // 土石方机械
            ("土石方机械", "挖掘机",       1),
            ("土石方机械", "推土机",       2),
            ("土石方机械", "装载机",       3),
            ("土石方机械", "压路机",       4),
            ("土石方机械", "平地机",       5),

            // 混凝土机械
            ("混凝土机械", "混凝土搅拌机", 1),
            ("混凝土机械", "混凝土泵车",   2),
            ("混凝土机械", "混凝土振捣棒", 3),

            // 桩工机械
            ("桩工机械", "旋挖钻机",       1),
            ("桩工机械", "振动打桩机",     2),
            ("桩工机械", "静压桩机",       3),

            // 高空作业机械
            ("高空作业机械", "剪叉式升降台",  1),
            ("高空作业机械", "直臂式高空车",  2),
            ("高空作业机械", "曲臂式高空车",  3),

            // 钢筋加工机械
            ("钢筋加工机械", "钢筋弯曲机",   1),
            ("钢筋加工机械", "钢筋切断机",   2),
            ("钢筋加工机械", "钢筋调直机",   3),

            // 焊接设备
            ("焊接设备", "电弧焊机",        1),
            ("焊接设备", "氩弧焊机",        2),
            ("焊接设备", "气体保护焊机",    3),

            // 动力与电气
            ("动力与电气", "发电机组",      1),
            ("动力与电气", "配电箱",        2),
            ("动力与电气", "电缆盘",        3),
        };

        var rootMap = roots.ToDictionary(r => r.Name, r => r.Id);

        foreach (var (parentName, childName, sort) in sub)
        {
            db.EquipmentCategories.Add(new EquipmentCategory
            {
                Name      = childName,
                Level     = 2,
                SortOrder = sort,
                ParentId  = rootMap[parentName]
            });
        }

        await db.SaveChangesAsync();
    }

    // ── 演示数据（开发/演示环境） ─────────────────────────────────────────────

    private static async Task SeedDemoDataAsync(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        if (await userManager.FindByNameAsync("demo.deviceadmin@equiprental.com") != null) return;

        var now   = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.Today);

        // ── Step 2: 演示用户 ──────────────────────────────────────────────────
        static ApplicationUser MakeUser(string email, string name) => new()
        {
            UserName = email, Email = email, RealName = name,
            IsActive = true, EmailConfirmed = true, CreatedAt = DateTime.UtcNow,
        };

        var deviceAdminUser    = MakeUser("demo.deviceadmin@equiprental.com",    "陈国梁");
        var dispatcherUser     = MakeUser("demo.dispatcher@equiprental.com",     "刘明远");
        var projectLeadUser    = MakeUser("demo.projectlead@equiprental.com",    "王建华");
        var safetyOfficerUser  = MakeUser("demo.safetyofficer@equiprental.com",  "张秀英");
        var auditorUser        = MakeUser("demo.auditor@equiprental.com",        "李文博");

        var demoUsers = new[]
        {
            (deviceAdminUser,   Roles.DeviceAdmin),
            (dispatcherUser,    Roles.Dispatcher),
            (projectLeadUser,   Roles.ProjectLead),
            (safetyOfficerUser, Roles.SafetyOfficer),
            (auditorUser,       Roles.Auditor),
        };

        foreach (var (user, role) in demoUsers)
        {
            var result = await userManager.CreateAsync(user, "Demo@123456");
            if (!result.Succeeded)
                throw new Exception($"创建演示用户 {user.Email} 失败：{string.Join(", ", result.Errors.Select(e => e.Description))}");
            await userManager.AddToRoleAsync(user, role);
        }

        // ── Step 3: 分类字典 ──────────────────────────────────────────────────
        var catMap = await db.EquipmentCategories
            .Where(c => c.Level == 2)
            .ToDictionaryAsync(c => c.Name, c => c.Id);

        // ── Step 4: 5 台设备 ──────────────────────────────────────────────────
        var eq1 = new Equipment
        {
            EquipmentNo = "DEMO-EQ-001", Name = "塔式起重机（演示A）",
            CategoryId = catMap["塔式起重机"], BrandModel = "中联重科 TC5610",
            ManufactureDate = today.AddYears(-5), FactoryNo = "FN-DEMO-EQ-001",
            TechSpecs = "最大起重量 6t，最大幅度 60m，独立起升高度 40m",
            OwnedBy = "演示租赁有限公司", Status = EquipmentStatus.Idle,
            CreatedById = deviceAdminUser.Id, CreatedAt = now.AddDays(-90),
        };
        var eq2 = new Equipment
        {
            EquipmentNo = "DEMO-EQ-002", Name = "挖掘机（演示B）",
            CategoryId = catMap["挖掘机"], BrandModel = "三一重工 SY215C",
            ManufactureDate = today.AddYears(-4), FactoryNo = "FN-DEMO-EQ-002",
            TechSpecs = "整机重量 21.5t，铲斗容量 0.93m³，发动机功率 122kW",
            OwnedBy = "演示租赁有限公司", Status = EquipmentStatus.InUse,
            CreatedById = deviceAdminUser.Id, CreatedAt = now.AddDays(-80),
        };
        var eq3 = new Equipment
        {
            EquipmentNo = "DEMO-EQ-003", Name = "混凝土泵车（演示C）",
            CategoryId = catMap["混凝土泵车"], BrandModel = "徐工 HB52K",
            ManufactureDate = today.AddYears(-3), FactoryNo = "FN-DEMO-EQ-003",
            TechSpecs = "臂架长度 52m，理论输出量 160m³/h，最大垂直泵送高度 170m",
            OwnedBy = "演示租赁有限公司", Status = EquipmentStatus.Maintenance,
            CreatedById = deviceAdminUser.Id, CreatedAt = now.AddDays(-70),
        };
        var eq4 = new Equipment
        {
            EquipmentNo = "DEMO-EQ-004", Name = "剪叉式升降台（演示D）",
            CategoryId = catMap["剪叉式升降台"], BrandModel = "鼎力 GTJZ0808",
            ManufactureDate = today.AddYears(-2), FactoryNo = "FN-DEMO-EQ-004",
            TechSpecs = "额定载荷 800kg，平台高度 8m，平台尺寸 2.26×0.81m",
            OwnedBy = "演示租赁有限公司", Status = EquipmentStatus.PendingReview,
            CreatedById = deviceAdminUser.Id, CreatedAt = now.AddDays(-3),
        };
        var eq5 = new Equipment
        {
            EquipmentNo = "DEMO-EQ-005", Name = "旋挖钻机（演示E）",
            CategoryId = catMap["旋挖钻机"], BrandModel = "宝峨 BG28",
            ManufactureDate = today.AddYears(-10), FactoryNo = "FN-DEMO-EQ-005",
            TechSpecs = "最大钻孔直径 1800mm，最大钻孔深度 60m，发动机功率 224kW",
            OwnedBy = "演示租赁有限公司", Status = EquipmentStatus.Scrapped,
            CreatedById = deviceAdminUser.Id, CreatedAt = now.AddDays(-365),
        };

        db.Equipments.AddRange(eq1, eq2, eq3, eq4, eq5);
        await db.SaveChangesAsync();

        // ── Step 5: 资质证书 ──────────────────────────────────────────────────
        db.Qualifications.AddRange(
            // EQ-001 (塔式起重机)
            new Qualification
            {
                EquipmentId = eq1.Id, Type = QualificationType.ProductCertificate,
                CertNo = "TCQZ-2021-001", IssuedBy = "国家市场监督管理总局",
                ValidFrom = today.AddYears(-3), ValidTo = today.AddYears(2), UpdatedAt = now,
            },
            new Qualification
            {
                EquipmentId = eq1.Id, Type = QualificationType.AnnualInspectionReport,
                CertNo = "TCNJ-2024-001", IssuedBy = "建筑起重机械检验机构",
                ValidFrom = today.AddYears(-1), ValidTo = today.AddYears(1), UpdatedAt = now,
            },
            // EQ-002 (挖掘机)
            new Qualification
            {
                EquipmentId = eq2.Id, Type = QualificationType.ProductCertificate,
                CertNo = "WJQZ-2022-002", IssuedBy = "工业和信息化部",
                ValidFrom = today.AddYears(-2), ValidTo = today.AddYears(3), UpdatedAt = now,
            },
            new Qualification
            {
                EquipmentId = eq2.Id, Type = QualificationType.InsuranceCertificate,
                CertNo = "WJBX-2025-002", IssuedBy = "中国人民财产保险股份有限公司",
                ValidFrom = today.AddMonths(-3), ValidTo = today.AddMonths(9), UpdatedAt = now,
            },
            // EQ-003 (混凝土泵车)
            new Qualification
            {
                EquipmentId = eq3.Id, Type = QualificationType.ProductCertificate,
                CertNo = "BCQZ-2023-003", IssuedBy = "工业和信息化部",
                ValidFrom = today.AddYears(-1), ValidTo = today.AddYears(4), UpdatedAt = now,
            },
            new Qualification
            {
                EquipmentId = eq3.Id, Type = QualificationType.AnnualInspectionReport,
                CertNo = "BCNJ-2024-003", IssuedBy = "建设机械检验机构",
                ValidFrom = today.AddMonths(-6), ValidTo = today.AddMonths(6), UpdatedAt = now,
            },
            // EQ-004 (剪叉升降台，待审，仍需提交)
            new Qualification
            {
                EquipmentId = eq4.Id, Type = QualificationType.ProductCertificate,
                CertNo = "JCQZ-2025-004", IssuedBy = "国家市场监督管理总局",
                ValidFrom = today.AddMonths(-1), ValidTo = today.AddYears(5), UpdatedAt = now,
            },
            // EQ-005 (旋挖钻机，已报废)
            new Qualification
            {
                EquipmentId = eq5.Id, Type = QualificationType.ProductCertificate,
                CertNo = "XWQZ-2015-005", IssuedBy = "工业和信息化部",
                ValidFrom = today.AddYears(-10), ValidTo = today.AddYears(-1), UpdatedAt = now,
            }
        );
        await db.SaveChangesAsync();

        // ── Step 6: 审核记录（EQ-001/002/003/005 通过，EQ-004 待审） ──────────
        db.AuditRecords.AddRange(
            new AuditRecord
            {
                EquipmentId = eq1.Id, AuditorId = auditorUser.Id,
                Action = AuditAction.Pass, Remark = "设备资质齐全，技术规格符合要求，审核通过。",
                AuditedAt = now.AddDays(-85),
            },
            new AuditRecord
            {
                EquipmentId = eq2.Id, AuditorId = auditorUser.Id,
                Action = AuditAction.Pass, Remark = "设备资质齐全，技术规格符合要求，审核通过。",
                AuditedAt = now.AddDays(-75),
            },
            new AuditRecord
            {
                EquipmentId = eq3.Id, AuditorId = auditorUser.Id,
                Action = AuditAction.Pass, Remark = "设备资质齐全，技术规格符合要求，审核通过。",
                AuditedAt = now.AddDays(-65),
            },
            new AuditRecord
            {
                EquipmentId = eq5.Id, AuditorId = auditorUser.Id,
                Action = AuditAction.Pass, Remark = "设备资质齐全，审核通过。（设备已超出使用年限，后续报废处理）",
                AuditedAt = now.AddDays(-360),
            }
        );
        await db.SaveChangesAsync();

        // ── Step 7: 调度申请 ──────────────────────────────────────────────────
        var reqA = new DispatchRequest
        {
            ProjectName = "北京朝阳高层住宅项目", ProjectAddress = "北京市朝阳区建设路88号",
            RequesterId = projectLeadUser.Id, CategoryId = catMap["塔式起重机"],
            Quantity = 1, ExpectedStart = today.AddDays(-60), ExpectedEnd = today.AddDays(-10),
            ContactName = "王建华", ContactPhone = "13900001001",
            SpecialRequirements = "需配备专职司机及信号工各一名",
            Status = DispatchRequestStatus.Scheduled, CreatedAt = now.AddDays(-65),
        };
        var reqB = new DispatchRequest
        {
            ProjectName = "上海浦东地铁站基坑开挖", ProjectAddress = "上海市浦东新区迎宾大道365号",
            RequesterId = projectLeadUser.Id, CategoryId = catMap["挖掘机"],
            Quantity = 1, ExpectedStart = today.AddDays(-20), ExpectedEnd = today.AddDays(10),
            ContactName = "王建华", ContactPhone = "13900001001",
            Status = DispatchRequestStatus.Scheduled, CreatedAt = now.AddDays(-22),
        };
        var reqC = new DispatchRequest
        {
            ProjectName = "广州南沙新区商业综合体", ProjectAddress = "广州市南沙区港前大道168号",
            RequesterId = projectLeadUser.Id, CategoryId = catMap["混凝土泵车"],
            Quantity = 1, ExpectedStart = today.AddDays(-15), ExpectedEnd = today.AddDays(15),
            ContactName = "王建华", ContactPhone = "13900001001",
            SpecialRequirements = "高层泵送，需要最大臂架规格",
            Status = DispatchRequestStatus.Scheduled, CreatedAt = now.AddDays(-17),
        };

        db.DispatchRequests.AddRange(reqA, reqB, reqC);
        await db.SaveChangesAsync();

        // ── Step 8: 调度订单 ──────────────────────────────────────────────────
        var orderA = new DispatchOrder
        {
            RequestId = reqA.Id, EquipmentId = eq1.Id, DispatcherId = dispatcherUser.Id,
            ActualStart = today.AddDays(-58), ActualEnd = today.AddDays(-12),
            UnitPrice = 1800.00m, Deposit = 5000.00m,
            VerifyCode = Guid.NewGuid().ToString(),
            Status = DispatchOrderStatus.Complete, CreatedAt = now.AddDays(-60),
        };
        var orderB = new DispatchOrder
        {
            RequestId = reqB.Id, EquipmentId = eq2.Id, DispatcherId = dispatcherUser.Id,
            ActualStart = today.AddDays(-18), ActualEnd = today.AddDays(12),
            UnitPrice = 2200.00m, Deposit = 8000.00m,
            VerifyCode = Guid.NewGuid().ToString(),
            Status = DispatchOrderStatus.InProgress, CreatedAt = now.AddDays(-20),
        };
        var orderC = new DispatchOrder
        {
            RequestId = reqC.Id, EquipmentId = eq3.Id, DispatcherId = dispatcherUser.Id,
            ActualStart = today.AddDays(-14), ActualEnd = today.AddDays(16),
            UnitPrice = 3500.00m, Deposit = 10000.00m,
            VerifyCode = Guid.NewGuid().ToString(),
            Status = DispatchOrderStatus.InProgress, CreatedAt = now.AddDays(-15),
        };

        db.DispatchOrders.AddRange(orderA, orderB, orderC);
        await db.SaveChangesAsync();

        // ── Step 9: 合同 ─────────────────────────────────────────────────────
        db.Contracts.AddRange(
            new Contract
            {
                OrderId = orderA.Id, ContractNo = "DEMO-CONTRACT-A-001",
                Status = ContractStatus.Signed, CreatedAt = now.AddDays(-59),
            },
            new Contract
            {
                OrderId = orderB.Id, ContractNo = "DEMO-CONTRACT-B-001",
                Status = ContractStatus.Signed, CreatedAt = now.AddDays(-19),
            },
            new Contract
            {
                OrderId = orderC.Id, ContractNo = "DEMO-CONTRACT-C-001",
                Status = ContractStatus.Signed, CreatedAt = now.AddDays(-14),
            }
        );
        await db.SaveChangesAsync();

        // ── Step 10: 进场核验 ─────────────────────────────────────────────────
        db.EntryVerifications.AddRange(
            new EntryVerification
            {
                OrderId = orderA.Id, VerifierId = deviceAdminUser.Id,
                IsPass = true, VerifiedAt = now.AddDays(-58),
            },
            new EntryVerification
            {
                OrderId = orderB.Id, VerifierId = deviceAdminUser.Id,
                IsPass = true, VerifiedAt = now.AddDays(-18),
            },
            new EntryVerification
            {
                OrderId = orderC.Id, VerifierId = deviceAdminUser.Id,
                IsPass = true, VerifiedAt = now.AddDays(-14),
            }
        );
        await db.SaveChangesAsync();

        // ── Step 11: 安全交底 + 参与人 ───────────────────────────────────────
        var briefingA = new SafetyBriefing
        {
            OrderId = orderA.Id, CreatorId = safetyOfficerUser.Id,
            BriefingDate = today.AddDays(-58), Location = "北京朝阳区建设路88号项目部",
            ContentHtml = "<p><strong>一、作业前检查</strong></p><p>每日作业前须检查钢丝绳、吊钩、制动器等关键部件，确认无异常方可作业。</p><p><strong>二、操作规程</strong></p><p>严禁超载起吊，严禁斜拉斜吊，严禁在强风（六级以上）天气作业。</p><p><strong>三、应急处置</strong></p><p>发生故障立即停机、切断电源，通知设备管理员，严禁擅自检修。</p>",
            Status = SafetyBriefingStatus.Completed, CreatedAt = now.AddDays(-58),
        };
        var briefingB = new SafetyBriefing
        {
            OrderId = orderB.Id, CreatorId = safetyOfficerUser.Id,
            BriefingDate = today.AddDays(-18), Location = "上海浦东新区迎宾大道365号工地",
            ContentHtml = "<p><strong>一、作业环境</strong></p><p>基坑作业区域已设置安全警戒线，严禁无关人员进入。</p><p><strong>二、操作规程</strong></p><p>挖掘作业时保持与管线安全距离不少于1m，地下障碍物须人工排查后方可机械开挖。</p><p><strong>三、个人防护</strong></p><p>作业人员须佩戴安全帽、安全带，操作室内不得饮食。</p>",
            Status = SafetyBriefingStatus.Completed, CreatedAt = now.AddDays(-18),
        };
        var briefingC = new SafetyBriefing
        {
            OrderId = orderC.Id, CreatorId = safetyOfficerUser.Id,
            BriefingDate = today.AddDays(-14), Location = "广州南沙区港前大道168号施工现场",
            ContentHtml = "<p><strong>一、高空作业安全</strong></p><p>泵管固定须牢固，高空拆装管道须系安全绳，严禁抛掷工具材料。</p><p><strong>二、设备操作</strong></p><p>泵车支腿须打在坚实地面，作业时严禁移动支腿。输送管堵塞时须泄压后方可处理。</p><p><strong>三、应急措施</strong></p><p>发现油管爆裂立即停机，通知维修人员，禁止用手堵压。</p>",
            Status = SafetyBriefingStatus.Completed, CreatedAt = now.AddDays(-14),
        };

        db.SafetyBriefings.AddRange(briefingA, briefingB, briefingC);
        await db.SaveChangesAsync();

        db.BriefingParticipants.AddRange(
            new BriefingParticipant
            {
                BriefingId = briefingA.Id, Name = "赵大伟", JobType = "塔吊司机",
                Phone = "13800001111", SignedById = projectLeadUser.Id, SignedAt = now.AddDays(-58),
            },
            new BriefingParticipant
            {
                BriefingId = briefingA.Id, Name = "钱小明", JobType = "信号工",
                Phone = "13800001112",
            },
            new BriefingParticipant
            {
                BriefingId = briefingB.Id, Name = "孙志强", JobType = "挖掘机操作员",
                Phone = "13800002111", SignedById = projectLeadUser.Id, SignedAt = now.AddDays(-18),
            },
            new BriefingParticipant
            {
                BriefingId = briefingB.Id, Name = "李建国", JobType = "安全监督员",
                Phone = "13800002112",
            },
            new BriefingParticipant
            {
                BriefingId = briefingC.Id, Name = "吴桂芳", JobType = "泵车操作员",
                Phone = "13800003111", SignedById = projectLeadUser.Id, SignedAt = now.AddDays(-14),
            },
            new BriefingParticipant
            {
                BriefingId = briefingC.Id, Name = "郑雪梅", JobType = "混凝土工班长",
                Phone = "13800003112",
            }
        );
        await db.SaveChangesAsync();

        // ── Step 12: 巡检记录（Chain A/B，不含 C） ────────────────────────────
        db.InspectionRecords.AddRange(
            new InspectionRecord
            {
                EquipmentId = eq1.Id, OrderId = orderA.Id, InspectorId = deviceAdminUser.Id,
                InspectionDate = today.AddDays(-13), OverallStatus = OverallInspectionStatus.Normal,
                Remark = "各机构运转正常，钢丝绳无断丝，吊钩无变形，制动器灵敏可靠。", CreatedAt = now.AddDays(-13),
            },
            new InspectionRecord
            {
                EquipmentId = eq2.Id, OrderId = orderB.Id, InspectorId = deviceAdminUser.Id,
                InspectionDate = today.AddDays(-5), OverallStatus = OverallInspectionStatus.Normal,
                Remark = "液压系统压力正常，油位适中，发动机运转平稳，斗齿磨损在允许范围内。", CreatedAt = now.AddDays(-5),
            }
        );
        await db.SaveChangesAsync();

        // ── Step 13: 故障报告（Chain C） ─────────────────────────────────────
        db.FaultReports.Add(new FaultReport
        {
            EquipmentId = eq3.Id, OrderId = orderC.Id, ReporterId = projectLeadUser.Id,
            Description = "主液压泵出现异常噪音，疑似密封圈损坏，工作油压下降明显，泵车输出量不足，已停止作业。",
            Severity = FaultSeverity.Severe,
            ReportedAt = now.AddDays(-3),
            Status = FaultStatus.InProgress,
        });
        await db.SaveChangesAsync();

        // ── Step 14: 退场申请 + 评价（仅 Chain A） ───────────────────────────
        var returnApp = new ReturnApplication
        {
            OrderId = orderA.Id, ApplicantId = projectLeadUser.Id,
            ActualReturnDate = today.AddDays(-12),
            ConditionDesc = "设备使用期间运转正常，无碰撞损伤，已完成清洁，完好归还。",
            Status = ReturnApplicationStatus.Complete, CreatedAt = now.AddDays(-12),
        };
        db.ReturnApplications.Add(returnApp);
        await db.SaveChangesAsync();

        db.ReturnEvaluations.Add(new ReturnEvaluation
        {
            ReturnAppId = returnApp.Id, EvaluatorId = deviceAdminUser.Id,
            AppearanceScore = 90, FunctionScore = 88,
            DamageDesc = null, Deduction = 0m, RefundAmount = 5000.00m,
            Remark = "设备外观良好，功能正常，全额退还押金。",
            EvaluatedAt = now.AddDays(-11),
        });
        await db.SaveChangesAsync();
    }
}
