# 13 资料索引

本文件列出写论文时最有价值的原始资料入口。

## 项目文档

| 文件 | 用途 |
|---|---|
| `../../docs/progress.md` | 当前开发完成情况和已验证功能 |
| `../../docs/PRD.md` | 产品背景、角色权限、业务需求 |
| `../../docs/architecture.md` | 系统架构、分层职责、模块依赖 |
| `../../docs/database.md` | 表结构、实体关系、状态枚举 |
| `../../docs/user-guide.md` | 各角色操作路径 |
| `../../docs/demo-guide.md` | 答辩演示流程和讲解重点 |
| `../../docs/deployment.md` | 部署和运行说明 |

## QA 与回归记录

| 文件 | 用途 |
|---|---|
| `../../docs/qa/e2e-run-20260420.md` | 端到端回归记录 |
| `../../docs/qa/e2e-run-20260422.md` | 端到端回归记录 |

## 已有论文辅助资料

| 文件夹 | 用途 |
|---|---|
| `../onboarding/` | 快速上手、答辩和业务演示 |
| `../code-reading/` | 源码阅读路线、模块映射、状态流转 |

## 关键源码入口

| 路径 | 用途 |
|---|---|
| `../../Program.cs` | 应用启动、服务注册、中间件、认证授权、数据库初始化 |
| `../../EquipmentRental.csproj` | 目标框架和 NuGet 依赖 |
| `../../Data/AppDbContext.cs` | EF Core 实体集合、关系、约束 |
| `../../Data/DbInitializer.cs` | 默认角色、账号和演示数据 |
| `../../Constants/Roles.cs` | 角色常量 |
| `../../Constants/InspectionChecklist.cs` | 固定巡检项 |
| `../../Models/Enums.cs` | 业务状态枚举 |
| `../../Models/Entities/` | 数据库实体 |
| `../../Models/ViewModels/` | 页面 ViewModel |
| `../../Controllers/` | Web 请求入口 |
| `../../Services/` | 业务规则和状态流转 |
| `../../Views/` | Razor 页面 |

## 核心服务索引

| 服务 | 论文用途 |
|---|---|
| `EquipmentService` | 设备入库、台账查询、Excel 导出 |
| `QualificationService` | 设备证件管理、到期预警 |
| `AuditService` | 资质审核、审核记录、通知 |
| `DispatchService` | 用车申请、可用设备筛选、调度单、合同 |
| `VerificationService` | 核验码、进场核验、核验记录 |
| `SafetyService` | 安全交底、富文本过滤、参与人签署、附件 |
| `InspectionService` | 巡检记录、固定检查项、照片 |
| `FaultService` | 故障上报、处理、关闭和设备状态恢复 |
| `ReturnService` | 退场申请、退场评价、押金扣款 |
| `DashboardService` | 首页统计、趋势、证件预警和待办 |
| `NotificationService` | 站内消息 |
| `FileService` | 文件上传安全校验和存储 |
| `UserService` | 用户管理和账号维护 |

## 使用建议

- 写论文前先读 `../../docs/PRD.md` 和 `../../docs/architecture.md`。
- 写数据库章节时对照 `../../docs/database.md` 和 `../../Data/AppDbContext.cs`。
- 写实现章节时对照 `../code-reading/04-module-map.md` 和 `04-source-evidence-map.md`。
- 写测试章节时对照 `../../docs/qa/` 下的回归记录。
- 让 ChatGPT 润色前先提供 `12-consistency-rules.md`，避免它把项目写偏。
