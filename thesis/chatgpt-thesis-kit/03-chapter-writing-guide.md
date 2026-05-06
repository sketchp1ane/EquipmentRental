# 03 章节写作指南

本文件说明每一章应该写什么，以及可引用哪些项目材料。

## 摘要

写作重点：

- 背景：建筑设备租赁管理仍存在信息分散和流程线下化问题。
- 方法：基于 ASP.NET Core MVC、EF Core、SQL Server 和 Identity 实现 B/S 管理系统。
- 成果：实现设备入库、资质审核、调度合同、进场核验、安全交底、巡检故障和退场评价。
- 价值：提高设备状态透明度、流程追溯性和权限安全性。

避免：

- 不要写“已在企业大规模应用”。
- 不要写项目未实现的移动端或硬件对接。

## 绪论

可用素材：

- `docs/PRD.md` 的项目背景和痛点。
- `01-project-facts.md` 的业务痛点。

写法建议：

先从行业背景讲到管理痛点，再引出系统建设目标。不要一开始就堆技术名词。

## 相关技术

可用素材：

- `Program.cs` 和 `EquipmentRental.csproj`。
- `01-project-facts.md` 的技术事实。

写法建议：

每种技术写“是什么 + 为什么适合本系统 + 在项目中的用途”。例如 ASP.NET Core MVC 用于构建服务端渲染的 B/S 管理系统，EF Core 用于数据访问和实体关系映射。

## 需求分析

可用素材：

- `docs/PRD.md`。
- `docs/user-guide.md`。
- `05-business-flow-material.md`。

写法建议：

按角色和模块展开。重点说明不同角色承担不同业务职责，系统通过权限控制实现操作边界。

## 系统总体设计

可用素材：

- `docs/architecture.md`。
- `07-architecture-material.md`。
- `04-source-evidence-map.md`。

写法建议：

突出 MVC 分层和 Service 层业务规则集中管理。写清楚 Controller 不直接处理复杂业务，Service 负责状态流转和事务。

## 数据库设计

可用素材：

- `docs/database.md`。
- `06-database-design-material.md`。
- `Models/Entities/`。
- `Data/AppDbContext.cs`。

写法建议：

先画或描述实体关系，再介绍核心表。重点写设备、调度单、合同、核验、安全交底、巡检、故障和退场之间的关联。

## 详细设计与实现

可用素材：

- `08-key-implementation-material.md`。
- `04-source-evidence-map.md`。
- `thesis/code-reading/05-request-flow-examples.md`。

写法建议：

选择 5 到 7 个核心功能深入写，不要平均铺开所有页面。每个功能按“业务目标 -> 页面入口 -> 控制器 -> 服务层规则 -> 数据状态变化 -> 结果”组织。

## 系统测试

可用素材：

- `10-testing-material.md`。
- `docs/qa/e2e-run-20260420.md`。
- `docs/qa/e2e-run-20260422.md`。

写法建议：

用表格列测试用例，包括测试模块、测试步骤、预期结果、实际结果。重点覆盖权限隔离、主业务流程和安全校验。

## 总结与展望

写作重点：

- 总结系统完成了全生命周期管理闭环。
- 总结技术上实现了 MVC 分层、RBAC、状态流转和安全上传。
- 不足可以写移动端支持、电子签章、GPS 定位、ERP 对接等。

注意：

展望必须用“后续可以扩展”，不能写成已实现。
