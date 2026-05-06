# 07 架构设计写作素材

## 总体架构

系统采用 ASP.NET Core MVC 架构，以浏览器作为客户端，通过 Controller 接收请求，使用 Service 处理业务逻辑，使用 EF Core 访问 SQL Server 数据库，最终通过 Razor View 返回页面。

```text
Browser
  -> Razor View / Form / AJAX
  -> Controller
  -> Service
  -> AppDbContext / EF Core
  -> SQL Server
```

## 分层职责

| 层 | 职责 | 论文说明 |
|---|---|---|
| Controller | 接收请求、授权、ModelState 验证、调用 Service、返回响应 | 保持 Web 层轻量 |
| Service | 业务规则、状态流转、事务、通知、审计日志 | 系统核心业务集中在此 |
| Entity | 数据表映射、导航属性 | 不放业务逻辑 |
| ViewModel | 页面输入输出、表单验证 | 避免直接暴露数据库实体 |
| Razor View | 页面展示、表单提交、局部视图复用 | 使用 Bootstrap 提供响应式界面 |
| AppDbContext | DbSet、实体关系、索引、约束 | 负责数据访问配置 |

## 依赖注入设计

业务服务在 `Program.cs` 中通过 `AddScoped` 注册，包括：

```text
EquipmentService
QualificationService
AuditService
DispatchService
VerificationService
SafetyService
InspectionService
FaultService
ReturnService
NotificationService
DashboardService
FileService
UserService
```

写作重点：

- 每个业务模块对应一个主要 Service。
- Controller 通过构造函数注入 Service。
- 依赖注入降低模块耦合，提高可维护性。

## 模块边界

| 模块 | 设计边界 |
|---|---|
| 设备台账 | 管理设备基础信息、分类、图片和导出 |
| 资质审核 | 管理设备证件、到期预警和审核状态 |
| 调度合同 | 处理用车申请、设备排期、合同生成和签署 |
| 进场核验 | 根据已签署调度单和核验码推进进场 |
| 安全监管 | 管理安全交底、巡检和故障工单 |
| 退场评价 | 管理退场申请、设备评分、扣款和后续状态 |
| 通知看板 | 聚合统计、待办提醒和站内消息 |

## 状态驱动设计

系统大量使用状态字段表达业务阶段，例如设备状态、调度单状态、合同状态、故障状态和退场申请状态。状态变化主要放在 Service 层，避免由页面直接决定业务结果。

这种设计的优点：

- 业务流程清晰。
- 状态变化可追踪。
- 便于权限控制和异常处理。
- 便于测试每个业务阶段。

## 可直接写入论文的段落

系统采用典型 MVC 分层架构。Controller 主要承担请求接收、权限验证和响应返回职责，不直接处理复杂业务规则；Service 层负责设备审核、调度排期、合同签署、进场核验、故障处理和退场评价等核心业务逻辑，并在必要时控制事务边界；Entity 层用于描述数据库表结构和实体关系；ViewModel 面向页面输入输出，降低页面与数据库实体之间的耦合。该分层方式使系统结构清晰，便于后续功能维护和扩展。
