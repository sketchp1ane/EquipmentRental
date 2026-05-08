# 文档事实一致性审计 — 2026-05-08

## 结论

本轮只校准项目说明文件，不修改业务代码、不修改 Razor 视图、不修改迁移和种子数据，也不修改 `thesis-writing/` 论文正文。

校准后的项目事实以当前源码为准，重点包括：

- 权限范围以 Controller 上的 `[Authorize]` 标注为准。
- 设备台账当前仅 `Admin / DeviceAdmin / Auditor` 可访问；设备证件与资质审核仅 `Admin / DeviceAdmin` 可访问。
- 合同扫描件上传后，当前实现会将 `Contract` 置为 `Signed`、`DispatchOrder` 置为 `Signed`，并将 `Equipment` 置为 `InUse`。
- 进场核验通过后，当前实现主要写入 `EntryVerification`，并将 `DispatchOrder` 推进为 `InProgress`。
- `ContractStatus.AwaitingSignature` 是枚举保留状态，当前主流程未实际写入。
- 设备分类当前作为种子数据预置的基础字典使用，没有独立分类 CRUD 页面。
- 文件上传限制统一为 `.jpg/.jpeg/.png/.pdf`，单文件最大 10 MB，扩展名、MIME 和文件魔数均需通过校验。

## 历史 QA 记录说明

以下文件保留为当时测试记录，不回写修改历史结论：

- `docs/qa/e2e-run-20260420.md`
- `docs/qa/e2e-run-20260422.md`
- `docs/qa/admin-flow-retest-20260507.md`

其中早期报告里关于 Auditor 审核权限、合同状态推进、设备状态推进、报表页面等内容，可能反映当时测试过程或旧文档口径。后续写论文、答辩或准备演示时，应以当前源码和本轮校准后的 `docs/PRD.md`、`docs/user-guide.md`、`docs/architecture.md`、`docs/database.md` 为准。
