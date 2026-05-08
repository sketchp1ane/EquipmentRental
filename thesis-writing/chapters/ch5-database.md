# 5 数据库设计

## 5.1 数据库设计目标与原则

数据库设计的目标是支撑建筑租赁设备从入库到退场的全过程管理。数据库不仅需要保存设备基础信息，还需要保存证件、审核、用车申请、调度单、合同、进场核验、安全交底、巡检、故障、退场评价、通知和操作日志等数据。数据库设计应保证业务链路可追溯、状态变化可记录、角色操作可关联。

关系数据库设计应关注数据完整性、实体关系、约束和索引等问题。Elmasri 和 Navathe 对关系数据库建模、实体联系模型和规范化设计进行了系统说明[24]；Silberschatz 等也指出，数据库系统需要通过模式设计、约束和事务机制保证数据一致性[25]。本系统采用 SQL Server 作为数据库，使用 EF Core Code First 方式建立实体与表结构之间的映射关系。

## 5.2 数据库概念结构设计

系统数据库以 Equipment 设备表为核心。设备与设备分类、设备图片、设备证件、审核记录和调度单等实体关联。调度单是连接租赁过程的关键实体，与用车申请、合同、进场核验、安全交底、巡检记录、故障工单和退场申请等实体关联。通过这种以设备为核心、以调度单串联租赁过程的设计，系统能够记录设备从入库、审核、调度、进场、使用到退场的完整轨迹。

【图5.1 占位：核心实体关系图】
图5.1 核心实体关系图

## 5.3 核心实体关系设计

系统用户实体 ApplicationUser 扩展自 Identity 用户，保存真实姓名、联系方式、启停状态等信息。用户与审核记录、用车申请、调度单、安全交底、巡检、故障、退场申请和退场评价等记录关联，用于实现责任追溯。

Equipment 表保存设备编号、设备名称、设备分类、品牌型号、所属公司和设备状态等信息，是系统最核心的数据表。EquipmentCategory 用于保存系统预置的层级设备分类基础字典，主要服务于设备入库时的分类选择、用车申请时的设备类型选择和设备列表筛选。因此，本文将设备分类定位为预置层级分类基础字典，而非独立分类管理模块。EquipmentImage 保存设备图片，Qualification 保存设备证件，AuditRecord 保存资质审核记录。设备审核通过后才进入空闲状态，才能参与后续调度。

DispatchRequest 表保存项目负责人提交的用车申请，DispatchOrder 表保存调度员生成的调度单。调度单与 Contract、EntryVerification、SafetyBriefing、InspectionRecord、FaultReport 和 ReturnApplication 等实体关联，承担租赁流程主线作用。Contract 表保存合同编号、合同状态和扫描件路径，用于记录合同草稿和签署扫描件，不涉及法律意义的在线电子签章。EntryVerification 表保存进场核验结果，SafetyBriefing 表保存安全交底内容，InspectionRecord 表保存巡检记录，FaultReport 表保存故障工单，ReturnApplication 和 ReturnEvaluation 表保存退场申请和评价结果。

## 5.4 主要数据表设计

表5.1 核心数据表说明表

| 实体/表 | 主要作用 | 关键字段 | 主要关联 |
|---|---|---|---|
| ApplicationUser | 系统用户表 | UserName、RealName、IsActive | 关联审核、调度、交底、故障、退场等操作记录 |
| EquipmentCategory | 预置层级设备分类基础字典 | Name、ParentId、Level | 与 Equipment 一对多，用于分类选择和筛选 |
| Equipment | 设备台账核心表 | EquipmentNo、Name、BrandModel、OwnedBy、Status | 关联证件、图片、审核、调度单 |
| EquipmentImage | 设备图片表 | EquipmentId、FilePath、UploadedAt | 关联 Equipment |
| Qualification | 设备证件表 | Type、CertNo、ValidTo、FilePath | 关联 Equipment |
| AuditRecord | 审核记录表 | Action、Remark、AuditedAt | 关联 Equipment 和 ApplicationUser |
| DispatchRequest | 用车申请表 | ProjectName、ExpectedStart、ExpectedEnd、Status | 关联申请人和设备分类 |
| DispatchOrder | 调度单表 | ActualStart、ActualEnd、UnitPrice、Deposit、VerifyCode、Status | 关联设备、申请、合同、核验等 |
| Contract | 合同草稿与扫描件记录表 | ContractNo、Status、ScanPath | 与 DispatchOrder 一对一 |
| EntryVerification | 进场核验表 | IsPass、FailReason、VerifiedAt | 与 DispatchOrder 一对一 |
| SafetyBriefing | 安全交底表 | BriefingDate、Location、ContentHtml、Status | 关联 DispatchOrder |
| InspectionRecord | 巡检记录表 | InspectionDate、OverallStatus、Remark | 关联 Equipment 和 DispatchOrder |
| FaultReport | 故障工单表 | Description、Severity、Status、Resolution | 关联 Equipment 和 DispatchOrder |
| ReturnApplication | 退场申请表 | ActualReturnDate、ConditionDesc、Status | 与 DispatchOrder 一对一 |
| ReturnEvaluation | 退场评价表 | AppearanceScore、FunctionScore、Deduction、RefundAmount | 与 ReturnApplication 一对一 |
| Notification | 站内消息表 | Title、Content、IsRead、RelatedUrl | 关联接收用户 |
| OperationLog | 操作日志表 | Action、EntityType、EntityId、OccurredAt | 关联操作用户 |

## 5.5 状态枚举、索引与约束设计

状态枚举是系统实现业务流程约束的重要基础。不同实体通过状态字段表达当前业务阶段，由服务层根据业务动作进行状态转换。

表5.2 主要状态枚举表

| 对象 | 状态 | 说明 |
|---|---|---|
| 设备 | 待审核、空闲、出租中/使用中、维修中、已报废 | 新建设备待审核，审核通过后空闲，合同扫描件上传后进入出租中/使用中 |
| 用车申请 | 待处理、已排期、已取消 | 用车申请被调度处理后变为已排期 |
| 调度单 | 待签署、已签署、进行中、已完成、已终止 | 合同扫描件上传后已签署，进场核验通过后进行中，退场评价后已完成 |
| 合同 | 草稿、待签署、已签署、已终止 | 当前主流程为“草稿 -> 已签署”；“待签署”为枚举保留状态，当前主流程未实际写入 |
| 安全交底 | 草稿、已完成 | 安全员和项目负责人完成系统内签署确认后可进入已完成 |
| 故障工单 | 待处理、处理中、已关闭 | 故障上报后待处理，设备管理员受理后处理中，关闭后已关闭 |
| 退场申请 | 待评价、已完成 | 退场评价提交后完成 |

ContractStatus 枚举包含 Draft、AwaitingSignature、Signed 和 Terminated 四种状态，分别对应草稿、待签署、已签署和已终止。需要说明的是，当前系统主流程中，调度排期后系统生成合同草稿，合同扫描件上传后直接将合同状态更新为已签署；AwaitingSignature 为保留枚举状态，当前主流程未实际写入。论文中的合同状态主流程应描述为“草稿 -> 已签署”，避免将保留枚举误写成实际业务流转节点。

在约束设计方面，设备编号、合同编号和核验码应具有唯一性，以避免业务记录混淆。调度单与合同、进场核验和退场申请存在一对一或强关联关系。证件有效期、调度日期、押金扣款等字段需要在服务层进行业务校验。系统还可对设备状态、证件有效期、调度单状态、通知接收人和操作日志时间等高频查询字段建立索引，以提高列表筛选、到期预警和历史追溯的查询效率。

## 5.6 本章小结

本章围绕系统数据存储需求进行了数据库设计。系统以设备为核心，以调度单串联合同、核验、安全交底、巡检、故障和退场等过程数据。通过实体关系、状态枚举、唯一约束和索引设计，数据库能够支持设备租赁全生命周期业务记录和过程追溯。

---
