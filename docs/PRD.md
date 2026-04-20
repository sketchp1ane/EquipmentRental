# 产品需求文档（PRD）
## 基于 ASP.NET MVC 的建筑租赁设备管理平台

**文档版本**：v1.1  
**创建日期**：2026-04-13  
**更新日期**：2026-04-16  
**作者**：Evan Chen  
**文档状态**：已完成

---

## 目录

1. [项目概述](#1-项目概述)
2. [用户角色与权限](#2-用户角色与权限)
3. [核心业务流程](#3-核心业务流程)
4. [功能模块需求](#4-功能模块需求)
5. [非功能性需求](#5-非功能性需求)
6. [技术架构](#6-技术架构)
7. [数据库设计（概要）](#7-数据库设计概要)
8. [页面/接口清单](#8-页面接口清单)
9. [约束与边界](#9-约束与边界)
10. [验收标准](#10-验收标准)

---

## 1. 项目概述

### 1.1 背景

随着城市化进程加速，建筑行业对塔吊、脚手架、挖掘机等大型设备的租赁需求持续增长。现有管理手段多依赖纸质台账或 Excel 表格，存在以下痛点：

- 设备状态不透明，出租/空闲/维修状态更新滞后；
- 租赁合同流转全靠线下，审批周期长、容易丢失；
- 设备进场核验依赖人工比对，效率低且易出错；
- 安全交底记录无法追溯，监管合规风险高；
- 退场评价缺乏标准化，设备损耗难以量化追责。

### 1.2 目标

构建一套 **B/S 架构的建筑租赁设备全生命周期数字化管理平台**，覆盖从设备入库登记到退场评价的完整业务链条，实现信息实时共享、流程线上流转、权限精细管控。

### 1.3 范围

| 在范围内 | 在范围外 |
|---|---|
| 设备台账管理 | GPS 实时轨迹追踪（硬件对接） |
| 设备入库与资质审核 | 财务对账与发票系统集成 |
| 线上调度与合同管理 | 移动端 App |
| 进场自动核验（二维码/编号扫描） | 第三方 ERP 集成 |
| 现场安全交底电子签名 | 多语言国际化 |
| 使用监管与故障上报 | |
| 退场评价与押金结算 | |
| 用户与权限管理 | |

---

## 2. 用户角色与权限

### 2.1 角色定义

| 角色 | 说明 | 典型操作 |
|---|---|---|
| **系统管理员** | 平台运维人员，拥有最高权限 | 用户管理、角色配置、系统参数设置 |
| **设备管理员** | 租赁公司内部设备主管 | 设备入库、资质审核、台账维护 |
| **调度员** | 负责设备排期与派单 | 线上调度、合同生成、调度日历 |
| **项目负责人** | 建筑工地甲方代表 | 提交用车申请、进场核验确认、退场评价 |
| **安全员** | 现场安全管理人员 | 安全交底填写与电子签名、故障上报 |
| **只读审计员** | 监管/审计人员 | 查看所有记录，无写权限 |

### 2.2 权限矩阵（CRUD，C=创建 R=查看 U=修改 D=删除）

| 模块 | 系统管理员 | 设备管理员 | 调度员 | 项目负责人 | 安全员 | 审计员 |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| 用户管理 | CRUD | - | - | - | - | R |
| 设备台账 | CRUD | CRUD | R | R | R | R |
| 资质审核 | CRUD | CRUD | R | R | - | R |
| 调度管理 | CRUD | R | CRUD | R | - | R |
| 合同管理 | CRUD | R | CRUD | R | - | R |
| 进场核验 | CRUD | R | R | CRU | R | R |
| 安全交底 | CRUD | R | R | R | CRUD | R |
| 故障上报 | CRUD | CRU | R | R | CRUD | R |
| 退场申请 | CRUD | R | R | CR | R | R |
| 退场评价 | CRUD | CRU | R | R | R | R |

---

## 3. 核心业务流程

```
设备入库登记
     │
     ▼
资质审核（合格证、年检报告、保险凭证）
     │ 审核通过
     ▼
设备台账（状态：空闲 / 出租 / 维修 / 报废）
     │
     │  项目负责人提交用车申请
     ▼
线上调度（调度员排期 → 生成调度单 → 生成租赁合同）
     │ 合同签署
     ▼
进场自动核验（扫码/编号比对，核验设备编号、有效期）
     │ 核验通过
     ▼
现场安全交底（安全员填写交底记录 → 相关方电子签名）
     │
     ▼
使用监管（日常巡检记录、故障上报、维修工单）
     │ 项目结束
     ▼
退场评价（设备状态评分、损耗记录、押金核算）
     │
     ▼
设备状态更新回台账（空闲 / 送修 / 报废）
```

---

## 4. 功能模块需求

### 4.1 用户与认证模块

#### 4.1.1 登录与注销
- 支持用户名+密码登录，密码使用 BCrypt 哈希存储。
- 登录失败连续 5 次后锁定账号 30 分钟，并记录日志。
- 支持"记住我"功能（持久化 Cookie，有效期 7 天）。
- 退出登录后销毁 Session，重定向至登录页。

#### 4.1.2 用户管理（系统管理员）
- 新增、编辑、停用/启用用户账号。
- 为用户分配一个或多个角色。
- 重置用户密码（发送邮件或直接设置临时密码）。
- 分页查询用户列表，支持按姓名、角色、状态筛选。

#### 4.1.3 个人信息
- 用户可修改自己的姓名、联系电话、邮箱。
- 用户可自行修改密码（需验证旧密码）。

---

### 4.2 设备台账模块

#### 4.2.1 设备分类管理
- 支持多级设备分类（例：起重机械 > 塔式起重机 > QTZ80）。
- 系统管理员可新增/编辑/禁用分类。

#### 4.2.2 设备入库登记
- 填写字段：设备名称、设备编号（唯一）、设备分类、品牌型号、出厂日期、出厂编号、额定载荷/技术参数、购置日期、原值（元）、所属公司、备注。
- 支持上传设备图片（最多 5 张，单张不超过 5 MB，格式 JPG/PNG）。
- 上传设备相关证件（PDF/图片，见 4.3）。
- 提交后状态置为"待审核"。

#### 4.2.3 设备台账查询
- 列表展示所有设备，支持按编号、名称、分类、状态、所属公司模糊/精确筛选。
- 导出筛选结果为 Excel（使用 EPPlus 库）。
- 点击设备进入详情页，展示完整信息与历史租赁记录。

#### 4.2.4 设备状态流转

```
待审核 ──审核通过──> 空闲 ──调度──> 出租中 ──退场──> 空闲
                              └──故障上报──> 维修中 ──维修完成──> 空闲
空闲/维修中 ──报废申请──审批──> 已报废
```

---

### 4.3 资质审核模块

#### 4.3.1 证件管理
- 每台设备需维护以下证件，每项含：证件类型、证件编号、签发机构、有效期起止、证件附件（PDF/图片）。
  - 产品合格证
  - 出厂检验报告
  - 特种设备使用登记证
  - 年度检验报告（最近一次）
  - 保险凭证
  - 安装（拆卸）资质证明（按需）
- 证件即将到期（距到期日 ≤ 30 天）时，系统自动在首页展示预警，并向设备管理员发送站内通知。
- 证件已过期的设备不允许被调度。

#### 4.3.2 入库审核流程
- 设备管理员提交入库后，由具有审核权限的设备管理员（或管理员）在审核列表中处理。
- 审核动作：通过 / 驳回（需填写驳回原因）。
- 审核结果通知提交人（站内消息）。
- 审核记录不可删除，留存审计日志。

---

### 4.4 线上调度模块

#### 4.4.1 用车申请
- 项目负责人填写：项目名称、项目地址、需求设备类型、数量、预计用车开始日期、预计归还日期、特殊要求、联系人、联系电话。
- 提交后进入调度员待处理列表。

#### 4.4.2 调度排期
- 调度员查看可用设备列表（已过资质审核、状态为"空闲"、无时间冲突）。
- 选择设备、确认用车周期、填写租金单价及押金，生成调度单。
- 调度日历视图：横轴为日期，纵轴为设备，直观展示占用情况。

#### 4.4.3 租赁合同
- 调度单确认后系统自动生成租赁合同草稿，包含：合同编号（系统生成）、甲乙方信息、设备信息、租期、租金、押金、违约条款（模板文本）。
- 支持在线预览（HTML 渲染）与 PDF 导出（使用 QuestPDF，纯 .NET 实现，macOS/Linux/Windows 均可运行）。
- 合同状态：草稿 → 待签署 → 已签署 → 已终止。
- 本期不实现电子签章，合同签署以"线下签署后上传扫描件"方式完成。
- **扫描件上传副作用**：`ContractService.UploadScanAsync` 在同一事务中把关联 `DispatchOrder.Status` 从 `Unsigned` 推进为 `Signed`；调度单必须进入 `Signed` 状态才会显示"进场核验码"区块，也才会允许进场核验（见 4.5）。

---

### 4.5 进场核验模块

#### 4.5.1 核验码生成
- 调度单进入 `Signed` 状态后（即合同扫描件已上传），系统在订单详情页展示唯一核验码（UUID）+ 二维码。未到 `Signed` 状态时核验码区块不可见，且 `VerificationService.VerifyAsync` 会直接拒绝 `Unsigned` 单的核验请求。

#### 4.5.2 核验操作
- 项目负责人（或指定人员）在进场时，在系统中输入核验码（或扫描二维码跳转），系统自动比对：
  - 设备编号是否与调度单一致；
  - 设备证件是否均在有效期内；
  - 设备状态是否为"出租中"；
  - 核验码是否未被使用且未过期（有效期为租赁开始日起 3 天）。
- 比对全部通过 → 核验状态标记为"已核验"，记录核验时间和操作人。
- 任一项不通过 → 提示具体失败原因，核验不通过，记录异常日志。

---

### 4.6 安全交底模块

#### 4.6.1 交底记录填写
- 设备核验通过后，安全员在系统中创建安全交底记录，填写：
  - 交底日期、交底地点
  - 作业内容描述
  - 安全注意事项（富文本，支持预置模板选择）
  - 参与人员名单（姓名+工种+联系电话）
  - 附件（照片或 PDF，最多 10 个文件）

#### 4.6.2 电子签名确认
- 各参与方（安全员、项目负责人）在系统中点击"确认签署"按钮，系统记录签署人账号、签署时间、客户端 IP，作为签名凭证。
- 所有必须签署方完成签署后，交底记录状态变为"已完成"，不可再修改（只读）。
- 支持导出交底记录为 PDF，供线下存档。

---

### 4.7 使用监管模块

#### 4.7.1 巡检记录
- 安全员可对出租中的设备创建日常巡检记录：巡检日期、巡检人、整体状态（正常/异常）、备注、现场照片。
- 巡检项固化为 **8 项**（整机外观 / 液压系统 / 电气系统 / 紧固件 / 安全装置 / 控制装置 / 作业环境 / 操作记录），每项结果入 `InspectionItemResults` 子表，状态可选 `Normal / Abnormal / NotApplicable` 三态；整体状态由各项聚合（任一项 `Abnormal` 即整体 `Abnormal`）。

#### 4.7.2 故障上报
- 安全员或项目负责人可提交故障工单：故障描述、发现时间、严重程度（轻微/中等/严重）、现场图片。
- 提交后系统自动将设备状态改为"维修中"（需设备管理员确认），并通知设备管理员。
- 设备管理员可在工单中填写处理结果、维修费用，并关闭工单，设备状态恢复为相应状态。

---

### 4.8 退场评价模块

#### 4.8.1 退场申请
- 项目负责人发起退场申请，填写：实际退场日期、设备总体状况描述。

#### 4.8.2 退场评价
- 设备管理员在验收设备后，填写评价：
  - 设备外观评分（1-5 分）
  - 功能完好性评分（1-5 分）
  - 损耗描述与照片
  - 损耗扣款金额（元）
  - 押金退还金额（= 押金 - 损耗扣款）
  - 综合评价备注
- 提交后通知项目负责人，退场流程结束，设备状态更新（空闲/送修/报废）。
- **扣款金额校验**：服务端拒绝 `Deduction < 0` 与 `Deduction > Deposit` 两种非法输入并返回字段级错误，不做静默钳位；`RefundAmount` 一律由服务端按 `Deposit - Deduction` 计算，不信任前端传值。

---

### 4.9 首页看板

所有已登录用户可见，内容按角色过滤：
- 设备总数 / 出租中 / 空闲 / 维修中 / 待审核 的数量卡片。
- 近 6 个月新增租赁笔数折线图。
- 即将到期证件预警列表（≤30 天）。
- 待处理事项入口（待审核设备、待处理故障、待签署合同等）。

---

## 5. 非功能性需求

### 5.1 安全性

| 项目 | 要求 |
|---|---|
| 认证 | ASP.NET Identity 框架，Session 有效期 30 分钟无操作自动过期 |
| 授权 | 基于角色的访问控制（RBAC），使用 `[Authorize(Roles="...")]` 装饰器，未授权请求返回 403 |
| 密码 | BCrypt 哈希，强度要求：至少 8 位，含大小写字母+数字 |
| 防注入 | 全部数据库操作使用 Entity Framework 参数化查询，禁止拼接 SQL |
| XSS | Razor 视图默认 HTML 编码，富文本字段使用 HtmlSanitizer 过滤 |
| CSRF | 所有表单使用 `@Html.AntiForgeryToken()`，POST/PUT/DELETE 验证 `[ValidateAntiForgeryToken]` |
| 文件上传 | 校验文件扩展名白名单（jpg/png/pdf）+ MIME 类型，文件存储在 Web 根目录之外 |
| 审计日志 | 记录所有增删改操作（操作人、时间、实体类型、变更内容摘要），日志只写不删 |
| HTTPS | 生产环境强制 HTTPS，开发环境可用 HTTP |

### 5.2 性能

- 列表页面加载时间（后端响应）：P95 < 1 秒（数据量 ≤ 10,000 条）。
- 文件上传最大支持单文件 20 MB（证件 PDF）。
- 数据库使用分页查询，每页默认 20 条，最大不超过 100 条。

### 5.3 可用性

- 界面基于 Bootstrap 5，支持响应式布局，兼容 1366×768 及以上分辨率的 PC 浏览器。
- 兼容目标浏览器：Chrome 最新版、Edge 最新版（IE 不作要求）。
- 表单提交失败时保留用户已输入内容，显示明确的错误提示（字段级）。

### 5.4 可维护性

- 遵循 MVC 分层原则，Controller 只负责请求路由与响应，业务逻辑封装在 Service 层。
- 数据库迁移使用 EF Core Migrations 管理，禁止手动执行 DDL。
- 敏感配置（数据库连接串、密钥）存放于 `appsettings.json` 的对应节，不硬编码于代码。
- 关键方法添加 XML 注释，便于生成文档。

---

## 6. 技术架构

### 6.1 技术选型

| 层次 | 技术 |
|---|---|
| 开发语言 | C# 12 / .NET 8（LTS） |
| Web 框架 | ASP.NET Core MVC 8 |
| 视图引擎 | Razor（.cshtml） |
| ORM | Entity Framework Core 8（Code First） |
| 数据库 | SQL Server 2022（Docker 容器，macOS 开发环境） |
| 前端样式 | Bootstrap 5 |
| 前端交互 | jQuery 3 + Bootstrap JS |
| 图表库 | Chart.js 4 |
| 富文本编辑 | Summernote |
| PDF 导出 | QuestPDF（纯 .NET，跨平台，无需系统依赖） |
| Excel 导出 | EPPlus 6 |
| 身份认证 | ASP.NET Core Identity |
| 版本控制 | Git / GitHub |
| 开发工具 | VS Code + C# Dev Kit 扩展（macOS） |

### 6.2 项目结构

```
EquipmentRental/
├── Controllers/          # MVC 控制器
│   ├── AccountController.cs
│   ├── EquipmentController.cs
│   ├── AuditController.cs
│   ├── DispatchController.cs
│   ├── ContractController.cs
│   ├── InspectionController.cs
│   ├── SafetyController.cs
│   ├── FaultController.cs
│   └── ReturnController.cs
├── Models/               # 实体模型 & ViewModel
│   ├── Entities/         # EF Core 实体
│   └── ViewModels/       # 视图专用 DTO
├── Services/             # 业务逻辑层
├── Data/                 # DbContext & Migrations
├── Views/                # Razor 视图
├── wwwroot/              # 静态资源（CSS/JS/图片）
├── Uploads/              # 上传文件存储（不对外暴露）
├── appsettings.json
└── Program.cs
```

### 6.3 部署架构（开发/验收环境）

```
浏览器
  │  HTTPS
  ▼
Kestrel（ASP.NET Core MVC 应用，`dotnet run`）
  │  EF Core
  ▼
SQL Server 2022（Docker：mcr.microsoft.com/mssql/server:2022-latest）
  │
文件系统（Uploads/）
```

---

## 7. 数据库设计（概要）

### 7.1 核心实体及关键字段

| 表名 | 关键字段 | 说明 |
|---|---|---|
| `Users` | Id, UserName, PasswordHash, PhoneNumber, IsActive | ASP.NET Identity 扩展 |
| `Roles` | Id, Name | 系统角色 |
| `EquipmentCategories` | Id, ParentId, Name, Level | 多级分类（自关联） |
| `Equipments` | Id, EquipmentNo(唯一), Name, CategoryId, BrandModel, ManufactureDate, Status(`PendingReview/Idle/InUse/Maintenance/Scrapped`), OwnedBy | 设备主表 |
| `EquipmentImages` | Id, EquipmentId, FilePath, UploadedAt | 设备图片 |
| `Qualifications` | Id, EquipmentId, Type, CertNo, IssuedBy, ValidFrom, ValidTo, FilePath | 设备证件 |
| `AuditRecords` | Id, EquipmentId, AuditorId, Action(通过/驳回), Remark, AuditedAt | 资质审核记录 |
| `DispatchRequests` | Id, ProjectName, ProjectAddress, RequesterId, EquipmentTypeNeeded, StartDate, EndDate, Status | 用车申请 |
| `DispatchOrders` | Id, RequestId, EquipmentId, DispatcherId, ActualStart, ActualEnd, UnitPrice, Deposit, Status, VerifyCode | 调度单 |
| `Contracts` | Id, DispatchOrderId, ContractNo, Status, ScanPath | 租赁合同 |
| `EntryVerifications` | Id, DispatchOrderId, VerifierId, VerifiedAt, IsPass, FailReason | 进场核验 |
| `SafetyBriefings` | Id, DispatchOrderId, CreatorId, BriefingDate, Location, ContentHtml, Status | 安全交底主表 |
| `BriefingParticipants` | Id, BriefingId, Name, JobType, Phone, SignedAt, SignedById | 交底参与人 |
| `InspectionRecords` | Id, EquipmentId, DispatchOrderId, InspectorId, InspectionDate, OverallStatus, Remark | 巡检记录 |
| `InspectionItemResults` | Id, InspectionId, ItemKey, Status(`Normal/Abnormal/NotApplicable`), Remark | 巡检 8 项每项结果 |
| `FaultReports` | Id, EquipmentId, DispatchOrderId, ReporterId, Description, Severity, ReportedAt, Status, Resolution | 故障工单 |
| `ReturnApplications` | Id, DispatchOrderId, ApplicantId, ActualReturnDate, EquipmentCondition, Status | 退场申请 |
| `ReturnEvaluations` | Id, ReturnAppId, EvaluatorId, AppearanceScore, FunctionScore, DamageDesc, Deduction, RefundAmount, EvaluatedAt | 退场评价 |
| `OperationLogs` | Id, UserId, Action, EntityType, EntityId, Detail, OccurredAt, ClientIp | 审计日志 |

### 7.2 主要关系

- `Equipments` 1 → N `Qualifications`
- `EquipmentCategories` 自关联（parentId）
- `DispatchOrders` 1 → 1 `EntryVerifications`
- `DispatchOrders` 1 → N `SafetyBriefings`
- `DispatchOrders` 1 → 1 `ReturnApplications`
- `ReturnApplications` 1 → 1 `ReturnEvaluations`
- `InspectionRecords` 1 → N `InspectionItemResults`

---

## 8. 页面/接口清单

### 8.1 页面清单（Razor View）

| URL 路径 | Controller/Action | 角色限制 | 说明 |
|---|---|---|---|
| `/` | Home/Index | 已登录 | 首页看板 |
| `/Account/Login` | Account/Login | 匿名 | 登录页 |
| `/Account/Logout` | Account/Logout | 已登录 | 退出 |
| `/Users` | Users/Index | 管理员 | 用户列表 |
| `/Users/Create` | Users/Create | 管理员 | 新增用户 |
| `/Equipment` | Equipment/Index | 已登录 | 设备台账列表 |
| `/Equipment/Create` | Equipment/Create | 设备管理员 | 设备入库登记 |
| `/Equipment/{id}` | Equipment/Details | 已登录 | 设备详情 |
| `/Equipment/{id}/Edit` | Equipment/Edit | 设备管理员 | 编辑设备 |
| `/Qualification/{equipmentId}` | Qualification/Index | 设备管理员 | 证件管理 |
| `/Audit` | Audit/Index | 设备管理员 | 待审核设备列表 |
| `/Audit/{id}/Review` | Audit/Review | 设备管理员 | 审核操作页 |
| `/Dispatch/Request` | Dispatch/Request | 项目负责人 | 提交用车申请 |
| `/Dispatch` | Dispatch/Index | 调度员/审计员 | 用车申请审批列表 |
| `/Dispatch/Orders` | Dispatch/Orders | 调度员/项目负责人/审计员 | 调度单列表 |
| `/Dispatch/Order?requestId={id}` | Dispatch/Order | 调度员 | 生成调度单 |
| `/Dispatch/OrderDetails/{id}` | Dispatch/OrderDetails | 调度员/项目负责人/审计员 | 调度单详情 |
| `/Dispatch/Calendar` | Dispatch/Calendar | 调度员 | 调度日历 |
| `/Contract/{id}` | Contract/Details | 调度员/项目负责人/审计员 | 合同详情 |
| `/Contract/{id}/Export` | Contract/Export | 调度员 | 导出合同 PDF |
| `/Contract/{id}/UploadScan` | Contract/UploadScan | 调度员 | 上传签署扫描件（联动订单 Signed） |
| `/Verification/Verify` | Verification/Verify | 项目负责人/管理员 | 进场核验操作 |
| `/Verification/List` | Verification/List | 项目负责人/调度员/审计员/管理员 | 核验记录 |
| `/Safety/List` | Safety/List | 安全员/项目负责人/审计员 | 安全交底列表 |
| `/Safety/Create?orderId={id}` | Safety/Create | 安全员 | 填写安全交底 |
| `/Safety/Details/{id}` | Safety/Details | 相关方 | 交底详情/签署 |
| `/Inspection` | Inspection/Index | 安全员/项目负责人/审计员 | 巡检记录列表 |
| `/Inspection/Create?orderId={id}` | Inspection/Create | 安全员/管理员 | 新增巡检记录 |
| `/Fault` | Fault/Index | 相关方 | 故障工单列表 |
| `/Fault/Create?orderId={id}` | Fault/Create | 安全员/项目负责人 | 故障上报 |
| `/Fault/Close/{id}` | Fault/Close | 设备管理员 | 关闭故障工单 |
| `/Return` | Return/Index | 项目负责人/设备管理员/审计员 | 退场列表 |
| `/Return/Apply?orderId={id}` | Return/Apply | 项目负责人 | 退场申请 |
| `/Return/Evaluate/{id}` | Return/Evaluate | 设备管理员 | 退场评价 |
| `/Return/Details/{id}` | Return/Details | 相关方 | 退场详情 |
| `/Notification/Recent` | Notification/Recent | 已登录 | 最近未读消息 JSON（铃铛） |
| `/Notification/MarkAllRead` | Notification/MarkAllRead | 已登录 | 一键全标已读 |
| `/Files/{id}` | Files/Download | 已登录（鉴权下发） | Uploads/ 下的附件流式下发 |

---

## 9. 约束与边界

1. **开发周期**：本项目为本科毕业设计，实际开发周期约 3–4 个月，功能优先级以核心业务流程（入库→调度→核验→交底→评价）为最高优先级。
2. **团队规模**：单人开发，不引入微服务或分布式架构。
3. **数据量假设**：设备数量 ≤ 1000 条，用户数 ≤ 100 人，不需要考虑大数据量优化（如分库分表）。
4. **电子签章**：本期不集成第三方 CA 电子签章，"签署"仅为系统内账号操作的记录留存，不具备法律效力。
5. **通知方式**：仅实现站内消息通知，不集成短信或邮件推送（邮件可选实现）。
6. **移动端**：仅需保证 PC 浏览器基本可用，不强制要求移动端完美适配。

---

## 10. 验收标准

### 10.1 功能验收

| 编号 | 验收项 | 通过条件 |
|---|---|---|
| F-01 | 用户登录 | 正确凭据可登录，错误凭据提示，5 次失败后锁定 |
| F-02 | 设备入库 | 必填字段校验，设备编号唯一，提交后状态为"待审核" |
| F-03 | 资质审核 | 审核通过/驳回均有记录，通过后设备状态变为"空闲" |
| F-04 | 证件到期预警 | ≤30 天到期的证件在首页展示预警 |
| F-05 | 调度排期 | 证件过期设备不可被选中；时间冲突设备不可被选中 |
| F-06 | 合同导出 | 合同可正常生成 PDF |
| F-07 | 进场核验 | 核验码正确时核验通过，字段不匹配时显示具体失败原因 |
| F-08 | 安全交底签署 | 所有必须签署方完成后状态变为"已完成"且不可修改 |
| F-09 | 故障上报 | 提交后设备状态自动变更，设备管理员收到站内通知 |
| F-10 | 退场评价 | 提交评价后押金退还金额正确计算，设备状态更新；扣款 > 押金或 < 0 时必须返回可见错误，不可写入 DB |
| F-11 | 权限隔离 | 无权限角色访问受保护页面返回 403 |

### 10.2 安全性验收

- 所有 POST 表单均有 CSRF Token 校验。
- 尝试 SQL 注入（`' OR 1=1 --`）不造成数据泄露。
- 非法文件类型（`.exe`、`.php`）上传被拒绝。
- 未登录用户直接访问受保护 URL 被重定向至登录页。

### 10.3 性能验收

- 设备台账列表页（含 500 条数据）响应时间 < 2 秒（开发机测试）。

---

*本文档为本科毕业设计产品需求文档，持续迭代更新，以最终提交版本为准。*
