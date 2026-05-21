# EquipmentRental 快速上手导读

这组文件用于论文写作、答辩准备和短时间熟悉项目。它不是替代 `docs/` 的完整文档，而是把最重要的信息重新排成“先能讲清楚、再能跑演示、最后能看代码”的路线。

## 推荐阅读路线

| 时间 | 目标 | 阅读顺序 |
|---|---|---|
| 30 分钟 | 能用几句话讲清系统 | `01-system-overview.md` -> `05-defense-cheatsheet.md` |
| 2 小时 | 能启动系统并跑一遍演示 | `01-system-overview.md` -> `02-run-and-demo.md` -> `03-business-flow-map.md` |
| 半天 | 能定位主要代码并回答实现问题 | 全部阅读，重点补 `04-code-navigation.md` 和 `06-defense-speech-and-qa.md` |

## 先记住这句话

EquipmentRental 是一个基于 ASP.NET Core MVC 的建筑租赁设备全生命周期管理平台，围绕“设备入库、资质审核、线上调度、进场核验、安全交底、使用监管、退场评价”七个阶段，把设备、合同、核验、安全和退场结算流程线上化。

## 这组文件怎么用

1. 先读 [01-system-overview.md](01-system-overview.md)，建立业务全局图。
2. 再按 [02-run-and-demo.md](02-run-and-demo.md) 启动项目，使用演示账号跑主流程。
3. 用 [03-business-flow-map.md](03-business-flow-map.md) 把页面操作和状态变化对上。
4. 用 [04-code-navigation.md](04-code-navigation.md) 知道每个功能应该去哪个 Controller、Service、Entity、View 看。
5. 最后用 [05-defense-cheatsheet.md](05-defense-cheatsheet.md) 准备答辩话术和常见问答。
6. 上台前用 [06-defense-speech-and-qa.md](06-defense-speech-and-qa.md) 练完整演讲稿和答问。

## 原始资料入口

| 文件 | 用途 |
|---|---|
| [../../README.md](../../README.md) | 项目启动、账号、技术栈总览 |
| [../../docs/PRD.md](../../docs/PRD.md) | 产品背景、角色权限、业务需求 |
| [../../docs/architecture.md](../../docs/architecture.md) | 系统分层、模块依赖、目录结构 |
| [../../docs/database.md](../../docs/database.md) | 数据库表结构、实体关系、状态枚举 |
| [../../docs/demo-guide.md](../../docs/demo-guide.md) | 答辩演示脚本 |
| [../../docs/progress.md](../../docs/progress.md) | 当前开发完成情况 |

## 快速检查清单

- 能说出 5 个角色：系统管理员、设备管理员、调度员、项目负责人、安全员。
- 能画出 7 阶段流程：入库 -> 审核 -> 调度 -> 核验 -> 交底 -> 监管 -> 退场。
- 能启动系统并登录至少 2 个演示账号。
- 能说明 Controller 负责请求，Service 负责业务规则，Entity 对应数据库表，ViewModel 对应页面表单。
- 能讲出 3 个技术亮点：RBAC 权限、文件安全上传、合同/PDF/二维码/富文本安全等任选。
