# 07 常见修改应该看哪里

这份文件用于遇到需求时快速定位，不用在项目里乱翻。

## 页面和表单

| 需求 | 优先看 |
|---|---|
| 新增/修改页面字段 | 对应 `Models/ViewModels/*ViewModels.cs` + `Views/<模块>/*.cshtml` |
| 修改列表筛选条件 | Controller 的 Index/List Action + Service 的列表查询方法 |
| 修改表单校验提示 | ViewModel 的 DataAnnotations + Controller 的 ModelState 处理 |
| 修改状态标签显示 | `Views/Shared/_StatusBadge.cshtml` 或具体页面中的状态渲染 |
| 修改分页 | `Views/Shared/_Pagination.cshtml` + 对应 List ViewModel |

## 业务规则

| 需求 | 优先看 |
|---|---|
| 设备入库默认状态 | `EquipmentService.CreateEquipmentAsync` |
| 审核通过/驳回规则 | `AuditService.PassAsync`、`AuditService.RejectAsync` |
| 可调度设备筛选 | `DispatchService.GetAvailableEquipmentsAsync`、`CreateOrderAsync` |
| 合同扫描件上传后合同、调度单、设备状态 | `DispatchService.UploadScanAsync` |
| 核验码规则 | `VerificationService.PerformVerifyAsync` |
| 安全交底完成规则 | `SafetyService.SignAsync` |
| 巡检项固定清单 | `Constants/InspectionChecklist.cs` + `InspectionService` |
| 故障关闭后设备恢复状态 | `FaultService.CloseFaultAsync` |
| 退场扣款和设备最终状态 | `ReturnService.CreateEvaluationAsync` |

## 权限和菜单

| 需求 | 优先看 |
|---|---|
| 改某个页面谁能访问 | 对应 Controller 的 `[Authorize(Roles = ...)]` |
| 新增角色或改角色名 | `Constants/Roles.cs`、`Data/DbInitializer.cs`、相关 Controller |
| 改侧边栏显示 | `Views/Shared/_SidebarNav.cshtml` |
| 改登录策略 | `Program.cs` Identity 配置 |
| 改账号启停/重置密码 | `UsersController` + `UserService` |

## 数据和状态

| 需求 | 优先看 |
|---|---|
| 新增数据库字段 | Entity + `AppDbContext` 配置 + ViewModel/View + Migration |
| 新增状态 | `Models/Enums.cs` + 所有 switch/状态标签/筛选下拉 |
| 改实体关系 | `Models/Entities` + `Data/AppDbContext.cs` |
| 改种子演示数据 | `Data/DbInitializer.cs` |
| 改首页统计 | `DashboardService` + `HomeViewModels.cs` + `Views/Home/Index.cshtml` |

## 文件和导出

| 需求 | 优先看 |
|---|---|
| 改允许上传的文件类型 | `FileService` 的扩展名、MIME、魔数表 |
| 改上传大小限制 | `FileService.MaxFileSizeBytes` |
| 改文件访问规则 | `FilesController.Get` |
| 改合同 PDF | `DispatchService` 内部的 `ContractDocument` |
| 改安全交底 PDF | `SafetyService.ExportPdfAsync` |
| 改 Excel 导出 | `EquipmentService.ExportToExcelAsync` |

## 一条实用定位公式

```text
要改页面显示
  -> View + ViewModel

要改提交行为
  -> Controller POST + Service

要改业务规则
  -> Service

要改数据结构
  -> Entity + AppDbContext + Migration

要改权限
  -> Controller Authorize + Sidebar
```
