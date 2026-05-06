# ChatGPT 毕业论文辅助资料包

这组文件专门用于把 EquipmentRental 项目的源码、业务流程、架构设计、数据库设计、安全机制和测试记录整理成 ChatGPT 可理解的论文写作上下文。

它不替代 `docs/`、`thesis/onboarding/` 或 `thesis/code-reading/`，而是把这些资料重新整理成“写毕业论文时可直接复制、引用、追问”的材料包。

## 推荐使用顺序

| 场景 | 先给 ChatGPT 的文件 |
|---|---|
| 第一次让 ChatGPT 理解项目 | `00-chatgpt-context-pack.md` |
| 生成论文目录 | `00-chatgpt-context-pack.md` + `02-thesis-outline.md` |
| 写某一章正文 | `00-chatgpt-context-pack.md` + `03-chapter-writing-guide.md` + 对应素材文件 |
| 写系统设计章节 | `07-architecture-material.md` + `06-database-design-material.md` |
| 写功能实现章节 | `08-key-implementation-material.md` + `04-source-evidence-map.md` |
| 写测试章节 | `10-testing-material.md` |
| 润色、降重、答辩问答 | `11-chatgpt-prompts.md` + `12-consistency-rules.md` |

## 文件说明

| 文件 | 用途 |
|---|---|
| `00-chatgpt-context-pack.md` | 一次性粘贴给 ChatGPT 的项目总上下文 |
| `01-project-facts.md` | 项目事实清单，防止论文口径漂移 |
| `02-thesis-outline.md` | 通用本科毕业论文目录建议 |
| `03-chapter-writing-guide.md` | 每章写作要点和素材入口 |
| `04-source-evidence-map.md` | 论文观点对应的源码位置 |
| `05-business-flow-material.md` | 业务流程和状态流转素材 |
| `06-database-design-material.md` | 数据库设计章节素材 |
| `07-architecture-material.md` | 架构设计章节素材 |
| `08-key-implementation-material.md` | 核心功能实现章节素材 |
| `09-security-material.md` | 权限、安全和文件上传素材 |
| `10-testing-material.md` | 测试章节素材 |
| `11-chatgpt-prompts.md` | 可复制的 ChatGPT 提示词 |
| `12-consistency-rules.md` | 术语、技术、功能边界统一规则 |
| `13-reference-index.md` | 原始文档和源码入口索引 |

## 使用原则

- 先给 ChatGPT 项目事实，再要求它写正文。
- 写正文时要求 ChatGPT “只基于提供的项目资料，不编造未实现功能”。
- 每章生成后，用 `12-consistency-rules.md` 检查术语、技术栈和功能边界。
- 涉及实现细节时，用 `04-source-evidence-map.md` 回到源码核对。

## 一句话项目定位

本项目是一个基于 ASP.NET Core MVC 的建筑租赁设备全生命周期管理平台，围绕“设备入库 -> 资质审核 -> 线上调度 -> 进场核验 -> 安全交底 -> 使用监管 -> 退场评价”七个阶段，实现设备、资质、合同、核验、安全监管和退场结算的线上化管理。
