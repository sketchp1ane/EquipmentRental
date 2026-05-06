# 06 数据库设计写作素材

## 数据库设计目标

数据库设计围绕设备租赁全生命周期展开，既要保存设备台账和资质信息，也要保存调度、合同、核验、安全、巡检、故障和退场等过程数据。设计重点是保证业务链路可追溯、状态变化可记录、角色操作可审计。

## 实体关系概览

```text
ApplicationUser
  -> DispatchRequest / DispatchOrder / AuditRecord / Notification / OperationLog

EquipmentCategory
  -> Equipment
       -> EquipmentImage
       -> Qualification
       -> AuditRecord
       -> DispatchOrder
            -> Contract
            -> EntryVerification
            -> SafetyBriefing
                 -> BriefingParticipant
                 -> BriefingAttachment
            -> InspectionRecord
                 -> InspectionImage
                 -> InspectionItemResult
            -> FaultReport
                 -> FaultImage
            -> ReturnApplication
                 -> ReturnEvaluation
```

## 核心表说明

| 表/实体 | 作用 |
|---|---|
| `ApplicationUser` | Identity 用户扩展，保存真实姓名、联系方式、启停状态 |
| `EquipmentCategory` | 设备分类，支持层级分类 |
| `Equipment` | 设备台账核心表，保存设备编号、名称、分类、型号、所属公司和状态 |
| `EquipmentImage` | 设备图片 |
| `Qualification` | 设备证件，包括合格证、年检报告、保险等 |
| `AuditRecord` | 资质审核记录 |
| `DispatchRequest` | 项目负责人提交的用车申请 |
| `DispatchOrder` | 调度员生成的调度单 |
| `Contract` | 与调度单一对一关联的租赁合同 |
| `EntryVerification` | 进场核验记录 |
| `SafetyBriefing` | 安全交底记录 |
| `BriefingParticipant` | 安全交底参与人和签署信息 |
| `BriefingAttachment` | 安全交底附件 |
| `InspectionRecord` | 巡检记录 |
| `InspectionItemResult` | 固定巡检项结果 |
| `FaultReport` | 故障工单 |
| `FaultImage` | 故障图片 |
| `ReturnApplication` | 退场申请 |
| `ReturnEvaluation` | 退场评价和押金扣款 |
| `Notification` | 站内消息 |
| `OperationLog` | 审计操作日志 |

## 关键状态枚举

| 枚举 | 状态 |
|---|---|
| `EquipmentStatus` | 待审核、空闲、出租中、维修中、已报废 |
| `DispatchRequestStatus` | 待处理、已排期、已取消 |
| `DispatchOrderStatus` | 待签署、已签署、进行中、已完成、已终止 |
| `ContractStatus` | 草稿、待签署、已签署、已终止 |
| `SafetyBriefingStatus` | 草稿、已完成 |
| `FaultStatus` | 待处理、处理中、已关闭 |
| `ReturnApplicationStatus` | 待评价、已完成 |

## 数据库设计特点

- 设备编号、合同编号、核验码等关键字段具有唯一性要求。
- 设备与证件、图片、审核记录、调度单等存在一对多关系。
- 调度单与合同、进场核验、退场申请等存在业务上的一对一或强关联关系。
- 安全交底、巡检和故障模块保留附件、图片和参与人等过程数据。
- 状态字段用于表达业务阶段，避免仅靠页面判断流程。
- 用户表与审核、调度、核验、通知、日志等记录关联，便于责任追溯。

## 可直接写入论文的段落

系统数据库采用 Code First 方式进行建模，核心实体围绕设备表展开。设备表保存设备编号、名称、分类、型号、所属公司和状态等基础信息，并与证件、图片、审核记录和调度单建立关联。调度单作为租赁过程中的关键业务实体，进一步关联合同、进场核验、安全交底、巡检记录、故障工单和退场评价等数据。通过这种以设备为核心、以调度单串联租赁过程的模型设计，系统能够完整记录设备从入库到退场的业务轨迹。
