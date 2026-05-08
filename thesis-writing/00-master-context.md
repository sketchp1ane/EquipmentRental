# 论文 AI 工作区总上下文

## 来源与边界

- 原始论文初稿：`/Users/sketchplane/Downloads/建筑租赁设备全生命周期管理平台论文初稿 (1).md`
- 当前修订稿来源：`/Users/sketchplane/Downloads/论文修订版.md`
- 仓库根目录：`/Users/sketchplane/EquipmentRental`
- 本工作区：`thesis-writing/`
- 拆分策略：修订稿按一级标题拆分，源论文文件不移动、不删除、不修改。
- 使用方式：后续 AI 写作、查证、改稿优先在本目录中进行；需要核对系统事实时，回到仓库源码与 `docs/` 文档取证。

## 项目背景

EquipmentRental 是一个面向建筑租赁设备全生命周期管理的 ASP.NET Core MVC 应用，覆盖设备入库、资质审核、线上调度、进场核验、安全交底、使用监管和退场评价等业务环节。论文主题为“建筑租赁设备全生命周期管理平台的设计与实现——基于 ASP.NET Core MVC”。

核心业务流程：

```text
设备入库 -> 资质审核 -> 线上调度 -> 进场核验 -> 安全交底 -> 使用监管 -> 退场评价
```

## 技术栈

- C# 13 / .NET 10 / ASP.NET Core MVC
- EF Core 10 + SQL Server
- ASP.NET Core Identity + RBAC + BCrypt 密码哈希
- Razor Views + Bootstrap 5 + jQuery 3 + Chart.js 4
- QuestPDF、EPPlus、QRCoder、HtmlSanitizer

## 仓库证据入口

- 产品需求与角色矩阵：`docs/PRD.md`
- 架构、分层职责与模块依赖：`docs/architecture.md`
- 数据库表结构、实体关系、状态枚举：`docs/database.md`
- 当前开发完成度：`docs/progress.md`
- 端到端与权限回归记录：`docs/qa/`
- 业务服务实现：`Services/`
- Web 控制层实现：`Controllers/`
- EF Core 实体：`Models/Entities/`
- 角色与巡检清单常量：`Constants/`
- 密码哈希实现：`Infrastructure/BCryptPasswordHasher.cs`

## 写作约束

- 论文正文应以“系统设计与实现”事实为准，不夸大为生产级商业系统。
- 涉及业务状态、角色权限、数据库字段、测试结论时，需要优先查 `docs/` 与源码。
- 控制器只描述为 Web 请求入口；业务规则、状态流转、事务边界应描述在服务层。
- 安全能力可表述为毕业设计场景下的基础防护，包括 CSRF、权限隔离、文件校验、富文本过滤、受控下载和密码哈希。
- 引用源码时优先选取核心方法片段，不大段粘贴全文件。

## 拆分映射

| 工作区文件 | 修订稿内容 |
|---|---|
| `00-front-matter.md` | 题名、说明、致谢、中英文摘要、关键词与分类号 |
| `01-outline.md` | “目次”与工作区文件映射 |
| `chapters/ch1-introduction.md` | 第 1 章 绪论 |
| `chapters/ch2-technology.md` | 第 2 章 相关技术介绍 |
| `chapters/ch3-requirements.md` | 第 3 章 系统需求分析 |
| `chapters/ch4-design.md` | 第 4 章 系统总体设计 |
| `chapters/ch5-database.md` | 第 5 章 数据库设计 |
| `chapters/ch6-implementation.md` | 第 6 章 系统详细设计与实现 |
| `chapters/ch7-testing.md` | 第 7 章 系统测试 |
| `chapters/ch8-conclusion.md` | 第 8 章 结论 |
| `04-references.md` | 参考文献 |
| `05-appendices.md` | 附录 A、附录 B |
| `06-thesis-metadata.md` | 学位论文数据集 |
