# Admin 单账号全流程复测（2026-05-07）

## 结论

- 结果：强通过。
- 账号：`admin@equiprental.com`，全程未切换业务账号。
- 环境：隔离库 `EquipmentRentalDb_AdminFlowRetest_20260507`，本地端口 `5086`。
- 验证范围：设备入库、资质、审核、用车申请、调度、合同、进场核验、安全交底、巡检、故障处理、退场申请、退场评价。

## 关键记录

- RunId：`ADMINFLOW-RETEST-20260507`
- Equipment：`#6` / `ADMINFLOW-RETEST-20260507-EQ`
- DispatchRequest：`#4`
- DispatchOrder：`#4`，最终状态 `Complete`
- Contract：`#4`，状态 `Signed`
- EntryVerification：`#4`
- SafetyBriefing：`#4`，状态 `Completed`
- InspectionRecord：`#3`
- FaultReport：`#2`，状态 `Closed`
- ReturnApplication：`#2`，状态 `Complete`
- ReturnEvaluation：`#3`，退款 `¥8,000.00`

## 退场评价入口回归

- Admin 在 `/Return/Details/2` 的待评价状态下可见 `填写退场评价` 按钮。
- 点击按钮进入 `/Return/Evaluate/2`，无 403/404。
- 提交评分、扣款 `0`、退场后状态 `空闲` 后，退场申请完成，订单完成，设备回到空闲。

## 备注

- 浏览器复测时发现旧 `localhost` Cookie 会携带上一轮测试库的用户 ID，导致新隔离库提交评价出现外键冲突；改用新登录态后流程正常。演示前建议重新登录或清理浏览器 Cookie。
- 本轮上传的临时文件已从 `Uploads/` 清理，隔离数据库保留用于复核。
