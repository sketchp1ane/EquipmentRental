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
│   ├── UserService.cs              # 用户管理
│   ├── NotificationService.cs      # 站内消息
│   └── FileService.cs              # 文件上传/下载
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
  └── EquipmentService（统计数量）
  └── QualificationService（到期预警）
  └── NotificationService（待办事项）

DispatchController
  └── DispatchService
        ├── EquipmentService（可用设备查询）
        └── QualificationService（证件有效期校验）

VerificationController
  └── VerificationService
        └── DispatchService（核验码比对）

ReturnController
  └── ReturnService
        └── EquipmentService（更新设备状态）
```
