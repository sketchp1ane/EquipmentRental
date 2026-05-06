# 03 数据模型和状态

## 核心实体关系

```text
ApplicationUser
  -> DispatchRequest / DispatchOrder / AuditRecord / Notification / OperationLog

EquipmentCategory
  -> Equipment
       -> EquipmentImage
       -> Qualification
       -> AuditRecord
       -> DispatchOrder
            -> Contract
            -> EntryVerification
            -> SafetyBriefing
                 -> BriefingParticipant
                 -> BriefingAttachment
            -> InspectionRecord
                 -> InspectionImage
                 -> InspectionItemResult
            -> FaultReport
                 -> FaultImage
            -> ReturnApplication
                 -> ReturnEvaluation
```

详细关系配置看 [../../Data/AppDbContext.cs](../../Data/AppDbContext.cs)，表结构说明可参考 [../../docs/database.md](../../docs/database.md)。

## 主要 `DbSet`

[../../Data/AppDbContext.cs](../../Data/AppDbContext.cs) 里显式声明了业务表：

```text
Equipments, EquipmentImages, Qualifications, AuditRecords
DispatchRequests, DispatchOrders, Contracts, EntryVerifications
SafetyBriefings, BriefingParticipants, BriefingAttachments
InspectionRecords, InspectionImages, InspectionItemResults
FaultReports, FaultImages
ReturnApplications, ReturnEvaluations
Notifications, OperationLogs
```

## 状态枚举速查

状态定义集中在 [../../Models/Enums.cs](../../Models/Enums.cs)。

| 枚举 | 状态 |
|---|---|
| `EquipmentStatus` | `PendingReview`、`Idle`、`InUse`、`Maintenance`、`Scrapped` |
| `DispatchRequestStatus` | `Pending`、`Scheduled`、`Cancelled` |
| `DispatchOrderStatus` | `Unsigned`、`Signed`、`InProgress`、`Complete`、`Terminated` |
| `ContractStatus` | `Draft`、`AwaitingSignature`、`Signed`、`Terminated` |
| `SafetyBriefingStatus` | `Draft`、`Completed` |
| `FaultStatus` | `Pending`、`InProgress`、`Closed` |
| `ReturnApplicationStatus` | `PendingEvaluation`、`Complete` |

## 设备状态主线

```text
PendingReview
  -> Idle          审核通过
  -> InUse         合同扫描件上传/进场核验相关流程
  -> Maintenance   故障上报
  -> Idle/InUse    故障关闭后按订单状态恢复
  -> Idle/Maintenance/Scrapped 退场评价时选择后续状态
```

状态改变主要看：

- [../../Services/AuditService.cs](../../Services/AuditService.cs)
- [../../Services/DispatchService.cs](../../Services/DispatchService.cs)
- [../../Services/VerificationService.cs](../../Services/VerificationService.cs)
- [../../Services/FaultService.cs](../../Services/FaultService.cs)
- [../../Services/ReturnService.cs](../../Services/ReturnService.cs)

## 调度和合同状态主线

```text
DispatchRequest.Pending
  -> Scheduled                调度员生成调度单

DispatchOrder.Unsigned
  -> Signed                   合同扫描件上传
  -> InProgress               进场核验通过
  -> Complete                 退场评价完成

Contract.Draft
  -> Signed                   上传扫描件
```

注意：当前源码里合同逻辑由 `ContractController` 调用 `DispatchService` 和 `FileService`，没有单独的 `ContractService` 文件。

## 索引和约束怎么看

[../../Data/AppDbContext.cs](../../Data/AppDbContext.cs) 里配置了关键约束：

| 约束 | 意义 |
|---|---|
| `Equipment.EquipmentNo` 唯一 | 设备编号不能重复 |
| `DispatchOrder.VerifyCode` 唯一 | 核验码唯一 |
| `Contract.OrderId` 唯一 | 一个调度单一个合同 |
| `EntryVerification.OrderId` 唯一 | 一个调度单一条核验记录 |
| `ReturnApplication.OrderId` 唯一 | 一个调度单一条退场申请 |
| `ReturnEvaluation.ReturnAppId` 唯一 | 一个退场申请一条评价 |
| `InspectionItemResult(InspectionId, ItemKey)` 唯一 | 每次巡检每个固定项只有一条结果 |
