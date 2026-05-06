# 04 模块源码地图

## 业务模块对应关系

| 模块 | Controller | Service | ViewModel | Views | 主要实体 |
|---|---|---|---|---|---|
| 登录/个人信息 | `AccountController` | `UserService` + Identity | `AccountViewModels.cs` | `Views/Account` | `ApplicationUser` |
| 用户管理 | `UsersController` | `UserService` | `UserViewModels.cs` | `Views/Users` | `ApplicationUser`、Identity Roles |
| 首页看板 | `HomeController` | `DashboardService` | `HomeViewModels.cs` | `Views/Home` | 多表聚合 |
| 设备台账 | `EquipmentController` | `EquipmentService` | `EquipmentViewModels.cs` | `Views/Equipment` | `Equipment`、`EquipmentImage`、`EquipmentCategory` |
| 证件管理 | `QualificationController` | `QualificationService` | `QualificationViewModels.cs` | `Views/Qualification` | `Qualification` |
| 资质审核 | `AuditController` | `AuditService` | `AuditViewModels.cs` | `Views/Audit` | `AuditRecord`、`Equipment` |
| 调度申请/排期 | `DispatchController` | `DispatchService` | `DispatchViewModels.cs` | `Views/Dispatch` | `DispatchRequest`、`DispatchOrder` |
| 合同 | `ContractController` | `DispatchService` + `FileService` | `ContractViewModels.cs` | `Views/Contract` | `Contract`、`DispatchOrder` |
| 进场核验 | `VerificationController` | `VerificationService` | `VerificationViewModels.cs` | `Views/Verification` | `EntryVerification` |
| 安全交底 | `SafetyController` | `SafetyService` | `SafetyViewModels.cs` | `Views/Safety` | `SafetyBriefing`、`BriefingParticipant`、`BriefingAttachment` |
| 巡检 | `InspectionController` | `InspectionService` | `InspectionViewModels.cs` | `Views/Inspection` | `InspectionRecord`、`InspectionItemResult`、`InspectionImage` |
| 故障 | `FaultController` | `FaultService` | `FaultViewModels.cs` | `Views/Fault` | `FaultReport`、`FaultImage` |
| 退场 | `ReturnController` | `ReturnService` | `ReturnViewModels.cs` | `Views/Return` | `ReturnApplication`、`ReturnEvaluation` |
| 文件访问 | `FilesController` | `FileService` | 无独立 ViewModel | 无页面 | `Uploads/` 文件 |
| 站内消息 | `NotificationController` | `NotificationService` | 局部 UI | `Views/Shared/_Notification.cshtml` | `Notification` |

## 按业务链读模块

```text
设备入库
  EquipmentController -> EquipmentService

证件维护
  QualificationController -> QualificationService

资质审核
  AuditController -> AuditService

用车申请/调度/合同
  DispatchController -> DispatchService
  ContractController -> DispatchService + FileService

进场核验
  VerificationController -> VerificationService

安全交底/巡检/故障
  SafetyController -> SafetyService
  InspectionController -> InspectionService
  FaultController -> FaultService

退场评价
  ReturnController -> ReturnService
```

## 按页面找代码

| URL 或页面 | 优先看 |
|---|---|
| `/Equipment` | `EquipmentController.Index`、`EquipmentService.GetPagedEquipmentsAsync`、`Views/Equipment/Index.cshtml` |
| `/Equipment/Create` | `EquipmentController.Create`、`EquipmentService.CreateEquipmentAsync` |
| `/Audit/Review` | `AuditController.Review`、`AuditService.PassAsync`、`AuditService.RejectAsync` |
| `/Dispatch/Request` | `DispatchController.DispatchRequest`、`DispatchService.SubmitRequestAsync` |
| `/Dispatch/Order` | `DispatchController.Order`、`DispatchService.CreateOrderAsync` |
| `/Contract/Details` | `ContractController.Details`、`DispatchService.GetContractDetailAsync` |
| `/Verification/Verify` | `VerificationController.Verify`、`VerificationService.PerformVerifyAsync` |
| `/Safety/Create` | `SafetyController.Create`、`SafetyService.CreateBriefingAsync` |
| `/Fault/Create` | `FaultController.Create`、`FaultService.CreateFaultAsync` |
| `/Return/Evaluate` | `ReturnController.Evaluate`、`ReturnService.CreateEvaluationAsync` |

## 读模块的固定动作

1. 打开 Controller，看允许哪些角色访问。
2. 找 GET Action，看页面初始数据如何准备。
3. 找 POST Action，看提交用哪个 ViewModel。
4. 跳到 Service，看业务校验和状态变化。
5. 回到 View，看表单字段、按钮和提示信息。
6. 最后看 Entity 和 DbContext，确认表关系。
