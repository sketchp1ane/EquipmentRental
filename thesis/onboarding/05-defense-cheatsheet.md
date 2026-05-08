# 05 答辩速查

## 60 秒项目介绍

本项目是一个基于 ASP.NET Core MVC 的建筑租赁设备全生命周期管理平台。它针对传统设备租赁中台账分散、合同流转慢、设备进场核验依赖人工、安全记录难追溯等问题，设计了从设备入库、资质审核、线上调度、进场核验、安全交底、使用监管到退场评价的完整业务链路。系统使用 SQL Server 和 EF Core 管理数据，使用 ASP.NET Core Identity 实现基于角色的权限控制，并通过 PDF/Excel 导出、二维码核验、文件安全上传和富文本安全过滤等功能提升业务闭环和安全性。

## 30 秒技术架构介绍

系统采用 MVC 分层架构：Controller 负责请求路由和权限控制，Service 负责业务规则、状态流转和事务处理，Entity 对应数据库表，ViewModel 面向页面输入输出。数据库使用 EF Core Code First，认证授权使用 ASP.NET Core Identity 和 BCrypt 密码哈希，前端使用 Razor、Bootstrap、jQuery 和 Chart.js。

## 最值得讲的 6 个亮点

| 亮点 | 怎么讲 |
|---|---|
| 全生命周期闭环 | 覆盖设备从入库到退场，不只是设备 CRUD |
| RBAC 权限控制 | 6 个角色，菜单、页面和操作按角色隔离 |
| 资质约束调度 | 设备需审核通过、证件有效、时间不冲突才能调度 |
| 合同和核验联动 | 合同扫描件上传后，合同和调度单签署、设备进入使用中，随后才允许进场核验 |
| 安全交底可追溯 | 富文本交底、参与人签署、附件和 PDF 留档 |
| 文件上传安全 | 白名单、MIME、魔数、大小限制、GUID 重命名、非 wwwroot 存储 |

## 常见问题参考

| 问题 | 回答要点 |
|---|---|
| 为什么选 ASP.NET Core MVC | 适合 B/S 管理系统，路由、权限、表单、服务端渲染和数据库集成成熟 |
| 为什么要分 Controller 和 Service | Controller 保持薄层，业务规则集中在 Service，便于维护和防止页面逻辑散落 |
| 系统怎么保证权限安全 | 使用 ASP.NET Core Identity + `[Authorize]`，角色常量统一管理，未授权访问会被拦截 |
| 上传文件为什么不放 `wwwroot` | 避免用户直接访问上传内容，统一通过 `FilesController` 鉴权下发 |
| 富文本有什么风险 | 富文本可能包含恶意 HTML，所以保存前使用 HtmlSanitizer 过滤 |
| 进场核验怎么避免乱用 | 只有已签署调度单才显示核验码并允许核验，服务层也会校验调度单状态 |
| 审计员能不能审核 | 不能。审计员是只读角色，用来查看历史和验证权限隔离 |
| 这个系统还有哪些可扩展方向 | 电子签章、移动端扫码、GPS 设备定位、财务发票、第三方 ERP 对接 |

## 演示时容易被问到的实现点

| 功能 | 代码入口 |
|---|---|
| 登录和锁定 | `AccountController`、`Program.cs` Identity 配置 |
| 权限角色 | `Constants/Roles.cs`、各 Controller 的 `[Authorize]` |
| 设备状态 | `Models/Enums.cs`、`EquipmentService` |
| 调度和合同 | `DispatchController`、`ContractController`、`DispatchService` |
| 核验码 | `VerificationController`、`VerificationService` |
| 安全交底 | `SafetyController`、`SafetyService` |
| 文件上传 | `FileService`、`FilesController` |
| 首页看板 | `HomeController`、`DashboardService` |

## 论文写作可用的小标题

- 建筑设备租赁管理现状与问题分析
- 建筑租赁设备全生命周期业务流程设计
- 基于 ASP.NET Core MVC 的系统总体架构设计
- 基于 RBAC 的多角色权限控制设计
- 设备资质审核与调度状态流转设计
- 进场核验、安全交底与退场评价模块实现
- 文件上传安全与富文本内容过滤设计
- 系统测试与答辩演示流程

## 最短答辩准备顺序

1. 背熟 60 秒项目介绍。
2. 画出七阶段业务流程。
3. 登录管理员、设备管理员、调度员、项目负责人、安全员账号各一次。
4. 重点演示设备台账、调度合同、核验码、安全交底、退场评价。
5. 准备回答“权限怎么做、状态怎么流转、文件上传怎么安全”这三个问题。
