# 系统架构

## 目录结构

```
EquipmentRental/
├── CLAUDE.md
├── Controllers/
│   ├── AccountController.cs        # 登录/注销/个人信息
│   ├── UsersController.cs          # 用户管理（管理员）
│   ├── EquipmentController.cs      # 设备台账 CRUD
│   ├── QualificationController.cs  # 证件管理
│   ├── AuditController.cs          # 资质审核
│   ├── DispatchController.cs       # 调度申请 + 排期
│   ├── ContractController.cs       # 合同详情 + PDF 导出 + 扫描件上传
│   ├── VerificationController.cs   # 进场核验
│   ├── SafetyController.cs         # 安全交底
│   ├── InspectionController.cs     # 日常巡检
│   ├── FaultController.cs          # 故障上报
│   ├── ReturnController.cs         # 退场评价
│   ├── ReportController.cs         # 统计报表
│   ├── NotificationController.cs   # 铃铛未读 / 全标已读
│   ├── FilesController.cs          # Uploads/ 鉴权下发
│   └── HomeController.cs           # 首页看板
├── Services/
│   ├── EquipmentService.cs
│   ├── QualificationService.cs
│   ├── AuditService.cs
│   ├── DispatchService.cs
│   ├── VerificationService.cs
│   ├── SafetyService.cs
│   ├── InspectionService.cs
│   ├── FaultService.cs
│   ├── ReturnService.cs
│   ├── ReportService.cs
│   ├── DashboardService.cs         # 首页看板聚合 / 角色待办分支
│   ├── UserService.cs              # 用户管理
│   ├── NotificationService.cs      # 站内消息
│   ├── ContractService.cs          # 合同生成 / PDF / 扫描件上传（联动订单 Signed）
│   └── FileService.cs              # 文件上传/下载（5 层校验 + GUID 重命名）
├── Models/
│   ├── Entities/                   # EF Core 实体（与数据库表一一对应）
│   └── ViewModels/                 # 视图专用 DTO（不直接暴露实体）
├── Data/
│   ├── AppDbContext.cs
│   └── Migrations/
├── Views/
│   ├── _ViewImports.cshtml             # 全局 using、TagHelper 注册
│   ├── _ViewStart.cshtml               # 指定默认 Layout
│   ├── Shared/
│   │   ├── _Layout.cshtml          # 主布局（含顶部导航、侧边栏）
│   │   ├── _Notification.cshtml    # 站内消息组件
│   │   └── Error.cshtml
│   ├── Home/
│   ├── Account/
│   ├── Equipment/
│   ├── Qualification/
│   ├── Audit/
│   ├── Dispatch/
│   ├── Contract/
│   ├── Verification/
│   ├── Safety/
│   ├── Inspection/
│   ├── Fault/
│   ├── Return/
│   ├── Report/
│   └── Users/
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── lib/                        # Bootstrap、jQuery、Chart.js（libman 管理）
├── Uploads/                        # 上传文件（不在 wwwroot，不对外暴露）
├── appsettings.json
├── appsettings.Development.json    # 本地数据库连接串（不提交 Git）
└── Program.cs
```

## 分层职责

| 层 | 职责 | 禁止 |
|---|---|---|
| Controller | 接收请求、调用 Service、映射 ViewModel、返回 View/JSON | 直接操作 DbContext、包含业务逻辑 |
| Service | 业务逻辑、事务管理、调用 DbContext | 直接构造 HttpContext、操作 Session |
| Model/Entity | 数据库映射，只含属性和导航属性 | 业务方法 |
| ViewModel | 视图数据传输，含 DataAnnotations 验证 | 数据库操作 |

## 角色常量

```csharp
public static class Roles
{
    public const string Admin        = "Admin";
    public const string DeviceAdmin  = "DeviceAdmin";
    public const string Dispatcher   = "Dispatcher";
    public const string ProjectLead  = "ProjectLead";
    public const string SafetyOfficer = "SafetyOfficer";
    public const string Auditor      = "Auditor";
}
```

## 服务注册（Program.cs 约定）

所有 Service 以 `AddScoped` 注册，`FileService` 和 `NotificationService` 同理。
EF Core DbContext、ASP.NET Core Identity 在 `Program.cs` 统一配置。

## 模块依赖关系

```
HomeController
  └── DashboardService（统计数量 + 到期预警 + 角色待办聚合）
        └── NotificationService

DispatchController
  └── DispatchService
        ├── EquipmentService（可用设备查询）
        └── QualificationService（证件有效期校验）

ContractController
  └── ContractService（扫描件上传时同事务把 DispatchOrder Unsigned → Signed）

VerificationController
  └── VerificationService（只允许 Signed 订单核验 → 核验通过时推进 InProgress + Equipment InUse）

FaultController
  └── FaultService（故障上报 Equipment InUse → Maintenance；关闭时恢复）
        └── EquipmentService

SafetyController
  └── SafetyService（双签完成 Draft → Completed）
        └── NotificationService

ReturnController
  └── ReturnService（评价提交 → Order Complete + Equipment 按填写状态翻转；扣款校验）
        └── EquipmentService

NotificationController ── NotificationService
FilesController ── FileService
```
