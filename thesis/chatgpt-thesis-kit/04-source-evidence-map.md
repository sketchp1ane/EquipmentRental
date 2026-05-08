# 04 源码证据地图

本文件用于把论文里的设计观点对应到源码位置。写实现章节时可以按这里查证。

## 应用入口

| 论文观点 | 源码证据 |
|---|---|
| 系统基于 ASP.NET Core MVC 构建 | `Program.cs` 中 `AddControllersWithViews` 和 `MapControllerRoute` |
| 系统使用 SQL Server 和 EF Core | `Program.cs` 中 `AddDbContext<AppDbContext>` 和 `UseSqlServer` |
| 业务服务通过依赖注入注册 | `Program.cs` 中多个 `AddScoped<...Service>()` |
| 全局启用 CSRF 防护 | `Program.cs` 中 `AutoValidateAntiforgeryTokenAttribute` |
| 启动时执行数据库迁移和种子数据初始化 | `Program.cs` 中 `MigrateAsync` 和 `DbInitializer.SeedAsync` |

## 分层结构

| 层 | 目录 | 论文中可说明 |
|---|---|---|
| Controller | `Controllers/` | 请求入口、授权、调用服务、返回页面 |
| Service | `Services/` | 业务规则、状态流转、事务、通知、审计 |
| Entity | `Models/Entities/` | 数据库实体与导航属性 |
| ViewModel | `Models/ViewModels/` | 页面输入输出 DTO 和 DataAnnotations 验证 |
| View | `Views/` | Razor 页面、表单、列表和详情 |
| DbContext | `Data/AppDbContext.cs` | 表集合、关系、索引和约束 |

## 模块源码映射

| 业务模块 | Controller | Service | 主要实体/ViewModel |
|---|---|---|---|
| 登录和个人信息 | `AccountController` | `UserService` + Identity | `ApplicationUser`、`AccountViewModels.cs` |
| 用户管理 | `UsersController` | `UserService` | `ApplicationUser`、`UserViewModels.cs` |
| 首页看板 | `HomeController` | `DashboardService` | `HomeViewModels.cs` |
| 设备台账 | `EquipmentController` | `EquipmentService` | `Equipment`、`EquipmentImage`、`EquipmentViewModels.cs` |
| 证件管理 | `QualificationController` | `QualificationService` | `Qualification`、`QualificationViewModels.cs` |
| 资质审核 | `AuditController` | `AuditService` | `AuditRecord`、`AuditViewModels.cs` |
| 调度申请和排期 | `DispatchController` | `DispatchService` | `DispatchRequest`、`DispatchOrder`、`DispatchViewModels.cs` |
| 合同管理 | `ContractController` | `DispatchService` + `FileService` | `Contract`、`ContractViewModels.cs` |
| 进场核验 | `VerificationController` | `VerificationService` | `EntryVerification`、`VerificationViewModels.cs` |
| 安全交底 | `SafetyController` | `SafetyService` | `SafetyBriefing`、`BriefingParticipant`、`SafetyViewModels.cs` |
| 巡检 | `InspectionController` | `InspectionService` | `InspectionRecord`、`InspectionItemResult`、`InspectionViewModels.cs` |
| 故障 | `FaultController` | `FaultService` | `FaultReport`、`FaultImage`、`FaultViewModels.cs` |
| 退场 | `ReturnController` | `ReturnService` | `ReturnApplication`、`ReturnEvaluation`、`ReturnViewModels.cs` |
| 文件访问 | `FilesController` | `FileService` | `Uploads/` 文件 |
| 站内消息 | `NotificationController` | `NotificationService` | `Notification` |

## 关键实现观点

| 论文观点 | 优先查看 |
|---|---|
| 新建设备后进入待审核状态 | `EquipmentService.CreateEquipmentAsync` |
| 审核通过后设备变为空闲 | `AuditService.PassAsync` |
| 调度前需要筛选可用设备 | `DispatchService.GetAvailableEquipmentsAsync`、`DispatchService.CreateOrderAsync` |
| 合同扫描件上传会推进合同、调度单和设备状态 | `DispatchService.UploadScanAsync` |
| 进场核验要求调度单已签署 | `VerificationService.PerformVerifyAsync` |
| 安全交底富文本需要过滤 | `SafetyService` 和 `Program.cs` 中的 `HtmlSanitizer` |
| 巡检项来自固定清单 | `Constants/InspectionChecklist.cs`、`InspectionService` |
| 故障上报会影响设备状态 | `FaultService.CreateFaultAsync`、`FaultService.CloseFaultAsync` |
| 退场评价会计算押金退还并更新设备状态 | `ReturnService.CreateEvaluationAsync` |
| 上传文件不放在 `wwwroot` 下直接访问 | `FileService`、`FilesController` |

## 写论文时的引用方式

建议写成：

```text
系统在 Web 层使用 Controller 接收请求，在 Service 层集中处理业务规则。例如调度模块由 DispatchController 接收用车申请和排期请求，再调用 DispatchService 完成可用设备筛选、调度单创建、合同生成和状态更新，从而避免业务逻辑散落在页面或控制器中。
```

避免写成：

```text
系统采用微服务架构。
```

本项目不是微服务架构。
