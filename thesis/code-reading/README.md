# EquipmentRental 源码阅读路线

这组文件专门帮助你快速读懂项目代码。已有的 `Thesis/onboarding/` 更偏答辩和业务演示；这里更偏源码入口、分层职责、模块映射、状态流转和常见修改定位。

## 三种读法

| 时间 | 目标 | 阅读顺序 |
|---|---|---|
| 1 小时 | 建立源码全局地图 | `01-project-entrypoints.md` -> `02-mvc-layering.md` -> `04-module-map.md` |
| 3 小时 | 能顺着业务流程读代码 | 上面 3 个 -> `03-data-model-and-status.md` -> `05-request-flow-examples.md` |
| 半天 | 能定位常见修改点 | 全部阅读，重点看 `06-security-and-permissions.md`、`07-where-to-change.md`、`08-reading-checklist.md` |

## 推荐读代码顺序

1. 从 [01-project-entrypoints.md](01-project-entrypoints.md) 看项目如何启动、服务如何注册。
2. 从 [02-mvc-layering.md](02-mvc-layering.md) 看 MVC 分层边界。
3. 从 [03-data-model-and-status.md](03-data-model-and-status.md) 看核心实体和状态枚举。
4. 从 [04-module-map.md](04-module-map.md) 找每个业务模块对应的 Controller、Service、ViewModel、View。
5. 从 [05-request-flow-examples.md](05-request-flow-examples.md) 顺三条真实请求链读代码。
6. 从 [06-security-and-permissions.md](06-security-and-permissions.md) 看权限和安全设计。
7. 从 [07-where-to-change.md](07-where-to-change.md) 学会遇到需求先定位文件。
8. 用 [08-reading-checklist.md](08-reading-checklist.md) 检查自己是否真正看懂。

## 先记住源码主线

```text
Program.cs
  -> 注册 EF Core / Identity / MVC / Services
  -> 默认路由进入 Controller
  -> Controller 接收请求和做角色授权
  -> Service 执行业务规则和状态流转
  -> AppDbContext 持久化 Entity
  -> ViewModel + Razor View 返回页面
```

## 重点源码入口

| 入口 | 作用 |
|---|---|
| [../../Program.cs](../../Program.cs) | 应用启动、依赖注册、中间件、数据库迁移和种子数据 |
| [../../Data/AppDbContext.cs](../../Data/AppDbContext.cs) | EF Core 表、关系、索引和约束 |
| [../../Data/DbInitializer.cs](../../Data/DbInitializer.cs) | 默认角色、账号和演示数据 |
| [../../Models/Enums.cs](../../Models/Enums.cs) | 业务状态枚举 |
| [../../Constants/Roles.cs](../../Constants/Roles.cs) | 角色常量 |
| [../../Controllers](../../Controllers) | Web 请求入口 |
| [../../Services](../../Services) | 业务规则集中位置 |

## 阅读原则

- 先读业务流程，再读具体方法。
- 先找 Controller Action，再顺着调用进入 Service。
- 状态变化优先看 Service，不要只看页面。
- 页面字段先找 ViewModel，再找 Razor 表单。
- 权限问题先看 `[Authorize]` 和 `Constants/Roles.cs`。
