# Auditor 角色移除验证 — 2026-05-09

## 变更范围

本次移除运行态 `Auditor` RBAC 角色，当前有效角色为：

- `Admin`
- `DeviceAdmin`
- `Dispatcher`
- `ProjectLead`
- `SafetyOfficer`

`AuditRecords.AuditorId` 保留为资质审核记录中的审核人用户 Id，不代表已移除的 RBAC 角色。

## 代码口径

- `Constants/Roles.cs` 不再定义 `Roles.Auditor`，`Roles.All` 仅返回 5 个角色。
- 用户管理仅允许 `Admin` 访问，用户创建/编辑角色选项不再出现 `Auditor`。
- 设备台账、调度、合同、核验、退场等授权标注均移除 `Auditor`。
- `DbInitializer` 不再创建旧 Auditor demo 账号，并在启动时幂等清理旧库中的 Auditor 角色关系。

## 验证项

- `dotnet build --no-restore -v minimal`：通过，0 warning / 0 error。
- 残留扫描：运行态代码不再包含 `Roles.Auditor` 或 `User.IsInRole(Roles.Auditor)`；扫描结果仅剩本说明和历史 QA 记录。
- 浏览器抽查：登录页、首页、用户管理、设备台账、资质审核、调度单、核验记录均可正常打开，无控制台错误。
- 旧 Auditor demo 账号登录失败，符合账号清理预期。
- `DeviceAdmin` 可访问资质审核页。

## 历史资料说明

旧 EF 迁移、历史 QA 报告和论文材料可能仍保留 `Auditor` 字段名或旧角色描述；这些文件按当时事实留存，不作为当前运行权限口径。
