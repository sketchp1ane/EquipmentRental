# 论文观点-源码证据表

| 论文位置 | 可支撑观点 | 主要证据 | 写作提示 |
|---|---|---|---|
| 第 1 章 绪论 | 建筑设备租赁管理存在台账分散、资质跟踪、合同流转、现场核验与安全追溯问题 | `docs/PRD.md` 第 1 节项目背景；第 3 节核心业务流程 | 背景论述可结合行业规范与本系统业务痛点，不把系统描述成已商用平台。 |
| 第 2 章 相关技术介绍 | 系统采用 B/S 架构与 MVC 分层思想 | `docs/architecture.md` 目录结构与分层职责；`Controllers/`、`Services/`、`Views/`、`Models/` | 强调 Controller、Service、Entity、ViewModel、Razor View 的职责划分。 |
| 第 2 章 相关技术介绍 | EF Core + SQL Server 适合本系统关系型业务数据持久化 | `docs/database.md`；`Data/AppDbContext.cs`；`Data/Migrations/` | 数据库章节引用实体关系与状态枚举，技术章节只写 ORM 与 Code First 的作用。 |
| 第 2 章 相关技术介绍 | Identity + RBAC 支撑认证与角色权限隔离 | `docs/PRD.md` 角色矩阵；`Constants/Roles.cs`；`Controllers/*Controller.cs` 授权标注；`Infrastructure/BCryptPasswordHasher.cs` | 避免泛泛写权限，具体落到系统管理员、设备管理员、调度员、项目负责人和安全员。 |
| 第 3 章 需求分析 | 系统核心角色和权限来自业务流程分工 | `docs/PRD.md` 用户角色与权限；`Constants/Roles.cs` | 权限矩阵以 PRD 和角色常量为事实来源。 |
| 第 3 章 需求分析 | 功能需求覆盖设备台账、资质审核、调度合同、进场核验、安全交底、巡检故障、退场评价 | `docs/PRD.md` 功能模块需求；`docs/progress.md` 完成项 | 可用 progress 证明模块已实现，用 PRD 证明需求来源。 |
| 第 4 章 总体设计 | 系统以服务层承载业务规则、状态流转和事务边界 | `docs/architecture.md` 分层职责和模块依赖；`Services/DispatchService.cs`、`Services/VerificationService.cs`、`Services/ReturnService.cs` | 写设计时突出“控制器薄、服务层厚”的实现约定。 |
| 第 4 章 总体设计 | 调度单串联合同、核验、安全交底、巡检、故障与退场评价 | `docs/architecture.md` 模块依赖关系；`docs/database.md` 实体关系概览 | 可配合 ER 图和状态流转图说明系统主线。 |
| 第 5 章 数据库设计 | 数据库围绕设备、调度单、合同、核验、交底、巡检、故障、退场等实体展开 | `docs/database.md` 表结构；`Models/Entities/` | 表结构应与 `docs/database.md` 保持一致，避免临时编字段。 |
| 第 5 章 数据库设计 | 状态枚举约束关键业务流转 | `docs/database.md` 状态枚举；`Models/Enums.cs` | 可列举 EquipmentStatus、DispatchOrderStatus、ContractStatus、SafetyBriefingStatus。 |
| 第 6 章 实现 | 合同扫描件上传会推进合同和调度单状态 | `Services/DispatchService.cs`；`Controllers/ContractController.cs`；`docs/PRD.md` 4.4.3 | 这是调度到核验的关键衔接，适合作为核心代码片段。 |
| 第 6 章 实现 | 进场核验要求调度单已签署，并在通过后更新调度单与设备状态 | `Services/VerificationService.cs`；`Controllers/VerificationController.cs` | 适合说明核验码、状态判断和业务写入。 |
| 第 6 章 实现 | 安全交底富文本和附件上传具备基础安全处理 | `Services/SafetyService.cs`；`Services/FileService.cs`；`docs/progress.md` 安全加固项 | 富文本写 HtmlSanitizer，附件写白名单、魔数、GUID 重命名和 Uploads 目录。 |
| 第 6 章 实现 | 首页看板聚合统计、证件预警和角色待办 | `Services/DashboardService.cs`；`Controllers/HomeController.cs`；`docs/progress.md` 首页看板 | 可以作为系统可视化与业务提醒实现证据。 |
| 第 7 章 测试 | 系统完成 E2E、权限隔离、文件与表单相关回归 | `docs/qa/e2e-run-20260420.md`；`docs/qa/e2e-run-20260422.md`；`docs/qa/admin-flow-retest-20260507.md` | 测试结论只写已验证内容，避免扩展到未测试性能指标。 |
| 第 8 章 结论 | 当前系统满足毕业设计场景的主要业务流程，但仍未接入移动端、硬件扫码、电子签章、GPS、ERP、财务系统 | `docs/PRD.md` 范围外事项；论文第 8 章原文 | 展望应与 PRD 范围边界一致。 |
