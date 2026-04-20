# 数据库设计

数据库：SQL Server 2022  
ORM：EF Core 8，Code First  
命名：表名复数 PascalCase，字段 PascalCase

---

## 实体关系概览

```
Users ──< UserRoles >── Roles

EquipmentCategories（自关联，树形）
     └──< Equipments
               ├──< EquipmentImages
               ├──< Qualifications
               ├──< AuditRecords
               ├──< DispatchOrders
               │         ├── EntryVerification
               │         ├──< SafetyBriefings >──< BriefingParticipants
               │         │                    └──< BriefingAttachments
               │         ├──< InspectionRecords >──< InspectionImages
               │         │                       └──< InspectionItemResults
               │         ├──< FaultReports >──< FaultImages
               │         └── ReturnApplication ──── ReturnEvaluation
               └──< InspectionRecords

Notifications（接收人 → Users）
OperationLogs（操作人 → Users）
```

---

## 表结构

### Users（ASP.NET Identity 扩展）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | nvarchar(450) | PK | Identity 默认 |
| UserName | nvarchar(256) | UNIQUE NOT NULL | 登录名 |
| PasswordHash | nvarchar(max) | NOT NULL | Identity 默认 PBKDF2，不要手动修改 |
| RealName | nvarchar(50) | NOT NULL | 真实姓名 |
| PhoneNumber | nvarchar(20) | | 联系电话 |
| Email | nvarchar(256) | | 邮箱 |
| IsActive | bit | NOT NULL DEFAULT 1 | 是否启用 |
| CreatedAt | datetime2 | NOT NULL | 创建时间 |

### EquipmentCategories
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| ParentId | int | FK(self) NULL | 父分类，NULL 表示顶级 |
| Name | nvarchar(50) | NOT NULL | 分类名称 |
| Level | int | NOT NULL | 层级（1/2/3） |
| SortOrder | int | NOT NULL DEFAULT 0 | 排序 |

### Equipments
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| EquipmentNo | nvarchar(50) | UNIQUE NOT NULL | 设备编号 |
| Name | nvarchar(100) | NOT NULL | 设备名称 |
| CategoryId | int | FK NOT NULL | 设备分类 |
| BrandModel | nvarchar(100) | NOT NULL | 品牌型号 |
| ManufactureDate | date | NOT NULL | 出厂日期 |
| FactoryNo | nvarchar(100) | | 出厂编号 |
| TechSpecs | nvarchar(500) | | 技术参数/额定载荷 |
| PurchaseDate | date | | 购置日期 |
| OriginalValue | decimal(12,2) | | 原值（元） |
| OwnedBy | nvarchar(100) | NOT NULL | 所属公司 |
| Status | int | NOT NULL DEFAULT 0 | 见下方枚举 |
| Remark | nvarchar(500) | | 备注 |
| CreatedById | nvarchar(450) | FK NOT NULL | 录入人 |
| CreatedAt | datetime2 | NOT NULL | |

**Status 枚举（`EquipmentStatus`）**：0=PendingReview 待审核 / 1=Idle 空闲 / 2=InUse 出租中 / 3=Maintenance 维修中 / 4=Scrapped 已报废

### EquipmentImages
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| EquipmentId | int | FK NOT NULL | |
| FilePath | nvarchar(500) | NOT NULL | 相对路径（Uploads/ 下） |
| UploadedAt | datetime2 | NOT NULL | |

### Qualifications（设备证件）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| EquipmentId | int | FK NOT NULL | |
| Type | int | NOT NULL | 见下方枚举 |
| CertNo | nvarchar(100) | | 证件编号 |
| IssuedBy | nvarchar(100) | | 签发机构 |
| ValidFrom | date | | 有效期起 |
| ValidTo | date | NOT NULL | 有效期止 |
| FilePath | nvarchar(500) | | 证件附件路径 |
| UpdatedAt | datetime2 | NOT NULL | |

**Type 枚举**：1=产品合格证 2=出厂检验报告 3=特种设备使用登记证 4=年度检验报告 5=保险凭证 6=安装资质证明

### AuditRecords（资质审核记录）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| EquipmentId | int | FK NOT NULL | |
| AuditorId | nvarchar(450) | FK NOT NULL | 审核人 |
| Action | int | NOT NULL | 1=通过 2=驳回 |
| Remark | nvarchar(500) | | 驳回原因 |
| AuditedAt | datetime2 | NOT NULL | |

### DispatchRequests（用车申请）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| ProjectName | nvarchar(100) | NOT NULL | |
| ProjectAddress | nvarchar(200) | NOT NULL | |
| RequesterId | nvarchar(450) | FK NOT NULL | 申请人 |
| CategoryId | int | FK NOT NULL | 需求设备类型 |
| Quantity | int | NOT NULL DEFAULT 1 | |
| ExpectedStart | date | NOT NULL | |
| ExpectedEnd | date | NOT NULL | |
| SpecialRequirements | nvarchar(500) | | |
| ContactName | nvarchar(50) | NOT NULL | |
| ContactPhone | nvarchar(20) | NOT NULL | |
| Status | int | NOT NULL DEFAULT 0 | 0=待处理 1=已排期 2=已取消 |
| CreatedAt | datetime2 | NOT NULL | |

### DispatchOrders（调度单）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| RequestId | int | FK NOT NULL | 对应用车申请 |
| EquipmentId | int | FK NOT NULL | |
| DispatcherId | nvarchar(450) | FK NOT NULL | 调度员 |
| ActualStart | date | NOT NULL | |
| ActualEnd | date | NOT NULL | |
| UnitPrice | decimal(10,2) | NOT NULL | 日租金（元/天） |
| Deposit | decimal(10,2) | NOT NULL | 押金 |
| VerifyCode | nvarchar(36) | UNIQUE NOT NULL | UUID，进场核验用 |
| Status | int | NOT NULL DEFAULT 0 | 见下方枚举 |
| CreatedAt | datetime2 | NOT NULL | |

**Status 枚举（`DispatchOrderStatus`）**：0=Unsigned 待签署 / 1=Signed 已签署 / 2=InProgress 进行中 / 3=Complete 已完成 / 4=Terminated 已终止

### Contracts（租赁合同）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| OrderId | int | FK UNIQUE NOT NULL | 1:1 调度单 |
| ContractNo | nvarchar(50) | UNIQUE NOT NULL | 系统生成 |
| Status | int | NOT NULL DEFAULT 0 | `ContractStatus`：0=Draft 草稿 / 1=AwaitingSignature 待签署 / 2=Signed 已签署 / 3=Terminated 已终止 |
| ScanPath | nvarchar(500) | | 线下签署扫描件路径；上传时同事务推进关联 DispatchOrder → Signed |
| CreatedAt | datetime2 | NOT NULL | |

### EntryVerifications（进场核验）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| OrderId | int | FK UNIQUE NOT NULL | 1:1 调度单 |
| VerifierId | nvarchar(450) | FK NOT NULL | 核验操作人 |
| VerifiedAt | datetime2 | NOT NULL | |
| IsPass | bit | NOT NULL | |
| FailReason | nvarchar(500) | | 失败原因 |

### SafetyBriefings（安全交底）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| OrderId | int | FK NOT NULL | |
| CreatorId | nvarchar(450) | FK NOT NULL | 安全员 |
| BriefingDate | date | NOT NULL | |
| Location | nvarchar(100) | NOT NULL | 交底地点 |
| ContentHtml | nvarchar(max) | NOT NULL | 富文本（已过 Sanitizer） |
| Status | int | NOT NULL DEFAULT 0 | `SafetyBriefingStatus`：0=Draft 草稿 / 1=Completed 已完成 |
| CreatedAt | datetime2 | NOT NULL | |

### BriefingParticipants（交底参与人）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| BriefingId | int | FK NOT NULL | |
| Name | nvarchar(50) | NOT NULL | |
| JobType | nvarchar(50) | NOT NULL | 工种 |
| Phone | nvarchar(20) | | |
| SignedById | nvarchar(450) | FK NULL | 系统账号（有账号时关联） |
| SignedAt | datetime2 | NULL | 签署时间，NULL=未签 |
| ClientIp | nvarchar(50) | | 签署时 IP |

### BriefingAttachments（安全交底附件）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| BriefingId | int | FK NOT NULL | 级联删除 |
| FilePath | nvarchar(500) | NOT NULL | Uploads/ 相对路径 |
| OriginalName | nvarchar(260) | NOT NULL | 原始文件名 |

### InspectionRecords（巡检记录）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| EquipmentId | int | FK NOT NULL | |
| OrderId | int | FK NOT NULL | |
| InspectorId | nvarchar(450) | FK NOT NULL | |
| InspectionDate | date | NOT NULL | |
| OverallStatus | int | NOT NULL | `OverallInspectionStatus`：0=Normal 正常 / 1=Abnormal 异常（由 `InspectionItemResults` 聚合而来） |
| Remark | nvarchar(500) | | |
| CreatedAt | datetime2 | NOT NULL | |

### InspectionImages（巡检现场照片）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| InspectionId | int | FK NOT NULL | |
| FilePath | nvarchar(500) | NOT NULL | Uploads/ 相对路径 |
| UploadedAt | datetime2 | NOT NULL | |

### InspectionItemResults（巡检项结果，8 项固定清单）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| InspectionId | int | FK NOT NULL | 级联删除 |
| ItemKey | nvarchar(50) | NOT NULL | 固定 8 项之一：Appearance/Hydraulic/Electrical/Fastener/SafetyDevice/ControlDevice/WorkEnv/OperationLog |
| Status | int | NOT NULL | `InspectionItemStatus`：0=Normal 正常 / 1=Abnormal 异常 / 2=NotApplicable 不适用 |
| Remark | nvarchar(500) | | 单项备注 |

### FaultReports（故障工单）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| EquipmentId | int | FK NOT NULL | |
| OrderId | int | FK NOT NULL | |
| ReporterId | nvarchar(450) | FK NOT NULL | |
| Description | nvarchar(500) | NOT NULL | |
| Severity | int | NOT NULL | 1=轻微 2=中等 3=严重 |
| ReportedAt | datetime2 | NOT NULL | |
| Status | int | NOT NULL DEFAULT 0 | `FaultStatus`：0=Pending 待处理 / 1=InProgress 处理中 / 2=Closed 已关闭 |
| Resolution | nvarchar(500) | | 处理结果 |
| RepairCost | decimal(10,2) | | 维修费用 |
| ClosedById | nvarchar(450) | FK NULL | |
| ClosedAt | datetime2 | NULL | |

### FaultImages（故障现场照片）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| FaultReportId | int | FK NOT NULL | |
| FilePath | nvarchar(500) | NOT NULL | Uploads/ 相对路径 |
| UploadedAt | datetime2 | NOT NULL | |

### ReturnApplications（退场申请）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| OrderId | int | FK UNIQUE NOT NULL | 1:1 调度单 |
| ApplicantId | nvarchar(450) | FK NOT NULL | 项目负责人 |
| ActualReturnDate | date | NOT NULL | |
| ConditionDesc | nvarchar(500) | | 设备状况描述 |
| Status | int | NOT NULL DEFAULT 0 | `ReturnApplicationStatus`：0=PendingEvaluation 待评价 / 1=Complete 已完成 |
| CreatedAt | datetime2 | NOT NULL | |

### ReturnEvaluations（退场评价）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| ReturnAppId | int | FK UNIQUE NOT NULL | 1:1 退场申请 |
| EvaluatorId | nvarchar(450) | FK NOT NULL | 设备管理员 |
| AppearanceScore | int | NOT NULL | 1-5 分 |
| FunctionScore | int | NOT NULL | 1-5 分 |
| DamageDesc | nvarchar(500) | | 损耗描述 |
| Deduction | decimal(10,2) | NOT NULL DEFAULT 0 | 扣款金额；服务端强制 `0 ≤ Deduction ≤ Deposit`，超界返回错误 |
| RefundAmount | decimal(10,2) | NOT NULL | 退还押金（= 押金 - 扣款），由服务端计算，不信任前端传值 |
| Remark | nvarchar(500) | | |
| EvaluatedAt | datetime2 | NOT NULL | |

### Notifications（站内消息）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | int | PK IDENTITY | |
| RecipientId | nvarchar(450) | FK NOT NULL | 接收人 |
| Title | nvarchar(100) | NOT NULL | |
| Content | nvarchar(500) | NOT NULL | |
| IsRead | bit | NOT NULL DEFAULT 0 | |
| RelatedUrl | nvarchar(200) | | 跳转链接 |
| CreatedAt | datetime2 | NOT NULL | |

### OperationLogs（审计日志，只写不删）
| 字段 | 类型 | 约束 | 说明 |
|---|---|---|---|
| Id | bigint | PK IDENTITY | |
| UserId | nvarchar(450) | FK NOT NULL | |
| Action | nvarchar(50) | NOT NULL | Create/Update/Delete/Approve 等 |
| EntityType | nvarchar(50) | NOT NULL | 实体名称 |
| EntityId | nvarchar(50) | NOT NULL | 被操作记录的主键 |
| Detail | nvarchar(1000) | | 变更摘要（JSON 或文本） |
| OccurredAt | datetime2 | NOT NULL | |
| ClientIp | nvarchar(50) | | |

---

## 枚举一致性说明

以上所有 `Status` / `OverallStatus` / `Type` / `Severity` / `Action` 字段的数值，均与 `Models/Enums.cs` 中的 C# 枚举一一对应。**数值顺序不得变更**（EF Core 按整数入库，改顺序会导致历史数据语义错位）；新增状态只允许追加到末尾。

---

## 索引建议

```sql
-- 高频查询字段
CREATE INDEX IX_Equipments_Status       ON Equipments(Status);
CREATE INDEX IX_Equipments_CategoryId   ON Equipments(CategoryId);
CREATE INDEX IX_Qualifications_ValidTo  ON Qualifications(ValidTo);   -- 到期预警
CREATE INDEX IX_DispatchOrders_Status   ON DispatchOrders(Status);
CREATE INDEX IX_DispatchOrders_Dates    ON DispatchOrders(ActualStart, ActualEnd); -- 冲突检测
CREATE INDEX IX_Notifications_Recipient ON Notifications(RecipientId, IsRead);
CREATE INDEX IX_OperationLogs_User      ON OperationLogs(UserId, OccurredAt);
```
