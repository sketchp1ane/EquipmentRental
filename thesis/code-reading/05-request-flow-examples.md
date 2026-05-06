# 05 三条真实请求流

这份文件用真实业务流程串代码。读的时候最好一边打开对应文件，一边按箭头跳转。

## 示例一：设备入库到审核通过

```text
Views/Equipment/Create.cshtml
  -> EquipmentController.Create POST
  -> EquipmentService.CreateEquipmentAsync
  -> 保存 Equipment / EquipmentImages
  -> Equipment.Status = PendingReview

Views/Qualification/Create.cshtml
  -> QualificationController.Create POST
  -> QualificationService.CreateAsync
  -> 保存 Qualification

Views/Audit/Review.cshtml
  -> AuditController.Review POST
  -> AuditService.PassAsync 或 RejectAsync
  -> Pass: Equipment.Status = Idle
  -> 写 AuditRecord / OperationLog / Notification
```

阅读重点：

- 设备编号唯一、图片上传、证件有效期等校验分别在哪一层做。
- 审核通过后为什么设备才能进入调度候选。
- 驳回为什么要求备注。

## 示例二：用车申请到合同签署再到进场核验

```text
Views/Dispatch/Request.cshtml
  -> DispatchController.DispatchRequestPost
  -> DispatchService.SubmitRequestAsync
  -> 保存 DispatchRequest，状态 Pending

Views/Dispatch/Order.cshtml
  -> DispatchController.Order POST
  -> DispatchService.CreateOrderAsync
  -> 校验设备 Idle、证件未过期、时间不冲突
  -> 保存 DispatchOrder，状态 Unsigned
  -> 保存 Contract，状态 Draft
  -> DispatchRequest.Status = Scheduled

Views/Contract/Details.cshtml
  -> ContractController.UploadScan POST
  -> DispatchService.UploadScanAsync
  -> FileService.SaveFileAsync
  -> Contract.Status = Signed
  -> DispatchOrder.Status = Signed
  -> Equipment.Status = InUse

Views/Verification/Verify.cshtml
  -> VerificationController.Verify POST
  -> VerificationService.PerformVerifyAsync
  -> 要求 DispatchOrder.Status = Signed
  -> 校验核验码有效期、设备状态、证件有效期
  -> EntryVerification.IsPass = true
  -> DispatchOrder.Status = InProgress
```

阅读重点：

- 合同逻辑当前在 `DispatchService`，不是独立 `ContractService`。
- `CreateOrderAsync` 使用执行策略和事务，确保调度单、合同、申请状态一起保存。
- `UploadScanAsync` 同时更新合同、调度单和设备状态。
- `PerformVerifyAsync` 会记录失败原因，也会处理重复核验场景。

## 示例三：故障上报、关闭和退场评价

```text
Views/Fault/Create.cshtml
  -> FaultController.Create POST
  -> FaultService.CreateFaultAsync
  -> 要求 DispatchOrder.Status = InProgress
  -> 保存 FaultReport / FaultImages
  -> Equipment.Status = Maintenance
  -> 通知设备管理员

Views/Fault/Details.cshtml
  -> FaultController.Accept POST
  -> FaultService.AcceptFaultAsync
  -> FaultStatus.Pending -> InProgress

Views/Fault/Details.cshtml
  -> FaultController.Close POST
  -> FaultService.CloseFaultAsync
  -> FaultStatus.InProgress -> Closed
  -> 如果订单仍 InProgress，设备恢复 InUse，否则恢复 Idle

Views/Return/Apply.cshtml
  -> ReturnController.Apply POST
  -> ReturnService.CreateApplicationAsync
  -> 保存 ReturnApplication，状态 PendingEvaluation

Views/Return/Evaluate.cshtml
  -> ReturnController.Evaluate POST
  -> ReturnService.CreateEvaluationAsync
  -> 校验扣款不能小于 0 且不能超过押金
  -> 保存 ReturnEvaluation
  -> ReturnApplication.Status = Complete
  -> DispatchOrder.Status = Complete
  -> Equipment.Status = 评价选择的新状态
```

阅读重点：

- 故障和退场都会影响设备状态，但入口角色不同。
- 维修关闭后设备状态由订单是否仍进行中决定。
- 退场评价负责押金退款金额计算和最终设备状态。

## 阅读技巧

- 先找 POST Action，再找 Service 的同名业务方法。
- 看到状态赋值就停下来，记录“从什么状态到什么状态”。
- 看到 `notificationService.SendAsync`，记录通知对象和触发条件。
- 看到 `OperationLog`，记录审计动作名。
