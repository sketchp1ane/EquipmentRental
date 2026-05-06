# 04 代码导航

这份文件用于快速看懂代码位置。核心原则：页面请求进 Controller，业务规则进 Service，数据库结构看 Entity 和 `AppDbContext`，页面表单看 ViewModel 和 Razor View。

## 先看这 5 个入口

| 入口 | 路径 | 看什么 |
|---|---|---|
| 应用启动 | `Program.cs` | EF Core、Identity、MVC、服务注册、中间件、种子数据 |
| 数据上下文 | `Data/AppDbContext.cs` | 实体集合、关系配置、索引、约束 |
| 种子数据 | `Data/DbInitializer.cs` | 默认角色、账号、演示数据 |
| 状态枚举 | `Models/Enums.cs` | 设备、调度单、合同、故障、退场等状态 |
| 角色常量 | `Constants/Roles.cs` | 授权中使用的角色名 |

## 分层怎么对应

| 层 | 目录 | 负责什么 | 不负责什么 |
|---|---|---|---|
| Controller | `Controllers/` | 接收请求、鉴权、调用 Service、返回 View/JSON | 不直接写业务规则 |
| Service | `Services/` | 状态流转、事务、校验、查询聚合 | 不直接依赖页面细节 |
| Entity | `Models/Entities/` | 数据库表映射和导航属性 | 不写业务方法 |
| ViewModel | `Models/ViewModels/` | 页面表单和展示 DTO | 不替代数据库实体 |
| View | `Views/` | Razor 页面、表单、列表、详情 | 不做核心业务判断 |

## 模块导航表

| 功能 | Controller | Service | ViewModel/View |
|---|---|---|---|
| 登录、个人信息 | `AccountController` | Identity / `UserService` | `AccountViewModels.cs`、`Views/Account/` |
| 用户管理 | `UsersController` | `UserService` | `UserViewModels.cs`、`Views/Users/` |
| 首页看板 | `HomeController` | `DashboardService` | `HomeViewModels.cs`、`Views/Home/` |
| 设备台账 | `EquipmentController` | `EquipmentService` | `EquipmentViewModels.cs`、`Views/Equipment/` |
| 证件管理 | `QualificationController` | `QualificationService` | `QualificationViewModels.cs`、`Views/Qualification/` |
| 资质审核 | `AuditController` | `AuditService` | `AuditViewModels.cs`、`Views/Audit/` |
| 调度申请/排期 | `DispatchController` | `DispatchService` | `DispatchViewModels.cs`、`Views/Dispatch/` |
| 合同预览/PDF/扫描件 | `ContractController` | `DispatchService` + `FileService` | `ContractViewModels.cs`、`Views/Contract/` |
| 进场核验 | `VerificationController` | `VerificationService` | `VerificationViewModels.cs`、`Views/Verification/` |
| 安全交底 | `SafetyController` | `SafetyService` | `SafetyViewModels.cs`、`Views/Safety/` |
| 巡检 | `InspectionController` | `InspectionService` | `InspectionViewModels.cs`、`Views/Inspection/` |
| 故障 | `FaultController` | `FaultService` | `FaultViewModels.cs`、`Views/Fault/` |
| 退场 | `ReturnController` | `ReturnService` | `ReturnViewModels.cs`、`Views/Return/` |
| 文件下载 | `FilesController` | `FileService` | 上传文件受控下发 |
| 站内消息 | `NotificationController` | `NotificationService` | `Views/Shared/_Notification.cshtml` |

## 一次请求大概怎么走

```text
浏览器提交表单
  -> Controller Action 接收 ViewModel
  -> ModelState 做基础验证
  -> Service 执行业务校验和状态流转
  -> AppDbContext 保存实体
  -> Controller 返回列表/详情/错误提示
```

例如“上传合同扫描件”：

```text
ContractController.UploadScan
  -> DispatchService.UploadScanAsync
  -> FileService 校验并保存文件
  -> 同步更新 Contract.Status 和 DispatchOrder.Status
  -> 返回合同详情页
```

## 看代码的最快方法

| 你想知道 | 先看 |
|---|---|
| 页面为什么只有某些角色能访问 | Controller 上的 `[Authorize(Roles = ...)]` |
| 某个按钮提交到哪里 | 对应 `.cshtml` 的 `asp-controller`、`asp-action` |
| 状态什么时候改变 | 对应 `Service` 的创建、审核、签署、核验、关闭、评价方法 |
| 表单字段从哪里来 | `Models/ViewModels/*ViewModels.cs` |
| 数据库字段和关系 | `Models/Entities/` + `Data/AppDbContext.cs` |
| 默认账号和演示数据 | `Data/DbInitializer.cs` |

## 答辩时可以这样解释架构

系统采用 ASP.NET Core MVC 分层结构。Controller 作为 Web 层只处理请求和响应，业务规则集中在 Service 层，例如调度、核验、退场评价等状态流转都由服务类完成；Entity 只描述数据库结构，ViewModel 用于页面输入输出，避免直接把数据库实体暴露给界面。这样可以降低耦合，也方便后续维护和测试。
