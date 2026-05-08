# 基于 ASP.NET Core MVC 的建筑租赁设备全生命周期管理平台的设计与实现

> 说明：本文件为 Markdown 论文修订稿，已根据审稿意见校准权限、状态流转、设备分类、文件上传和测试用例等关键口径。正式提交前，请根据学校 Word 模板补充封面、封二、题名页、页眉页码、目录自动生成、图表样式、三线表、参考文献悬挂缩进等格式。  
> 图像位置暂用图占位表示，正式排版时应替换为实际图片并保留规范图题。  
> 参考文献均选用真实存在的标准、经典书籍、经典论文或官方文档。电子文献访问日期暂按 2026-05-06 书写，提交前建议按实际访问日期核验和调整。

---

# 致谢

本论文是在指导教师的悉心指导下完成的。从毕业设计选题、需求分析、系统设计、功能实现到论文撰写，指导教师均给予了耐心细致的帮助和建议，使我能够更加系统地理解软件工程项目从需求到实现再到测试的完整过程。在此，谨向指导教师表示衷心感谢。

同时，感谢学院各位老师在本科阶段的教学和培养，使我掌握了 Web 开发、数据库设计、软件工程、信息系统分析等方面的专业知识，为本课题的完成奠定了基础。感谢同学们在系统测试、页面体验和论文修改过程中提出的意见与建议。最后，感谢家人在学习和生活中给予的支持与鼓励，使我能够顺利完成本次毕业设计和论文写作。

---

# 摘要

随着建筑施工项目规模和设备租赁需求的增长，建筑租赁设备管理面临设备台账分散、资质证件难以及时跟踪、合同流转依赖线下、进场核验效率较低、安全交底和故障记录难追溯等问题。针对上述问题，本文设计并实现了一个基于 ASP.NET Core MVC 的建筑租赁设备全生命周期管理平台，用于支持设备从入库登记、资质审核、线上调度、合同管理、进场核验、安全交底、使用监管到退场评价的完整业务流程。

系统采用 B/S 架构，以 ASP.NET Core MVC 作为后端 Web 框架，使用 EF Core 和 SQL Server 完成数据持久化，结合 ASP.NET Core Identity 和基于角色的访问控制实现用户认证与权限隔离。前端采用 Razor Views、Bootstrap、jQuery 和 Chart.js 完成页面展示与交互，系统还集成 QuestPDF、EPPlus、QRCoder 和 HtmlSanitizer 等组件，实现合同导出、台账导出、核验码展示和富文本过滤等功能。系统按照 Controller、Service、Entity、ViewModel、Razor View 和 AppDbContext 进行分层设计，将业务规则、状态流转、文件安全和通知逻辑集中在服务层处理。

系统实现了用户与认证、设备台账、资质审核、线上调度、合同管理、进场核验、安全交底、巡检监管、故障处理、退场评价、首页看板、站内消息和文件访问等模块。测试结果表明，系统能够完成建筑租赁设备主要业务流程，关键状态能够按照预期流转，角色权限隔离和文件上传安全校验能够满足毕业设计场景下的使用要求。

关键词：建筑租赁设备；全生命周期管理；ASP.NET Core MVC；EF Core；RBAC；SQL Server

中图分类号：TP311

---

# Abstract

With the growth of construction projects and equipment rental demand, the management of construction rental equipment faces several practical problems, such as scattered equipment ledgers, difficulty in tracking qualification documents, offline contract circulation, inefficient entry verification, and poor traceability of safety briefings and fault records. To address these issues, this thesis designs and implements a full-life-cycle management platform for construction equipment rental based on ASP.NET Core MVC. The platform supports the complete business process from equipment registration, qualification review, online scheduling, contract management, entry verification, safety briefing, usage supervision, to return evaluation.

The system adopts a Browser/Server architecture. ASP.NET Core MVC is used as the Web framework, while EF Core and SQL Server are used for data persistence. ASP.NET Core Identity and role-based access control are adopted to implement user authentication and permission isolation. Razor Views, Bootstrap, jQuery, and Chart.js are used for page rendering and interaction. In addition, QuestPDF, EPPlus, QRCoder, and HtmlSanitizer are integrated to support contract export, ledger export, verification code display, and rich-text filtering. The system is designed with layered responsibilities, including Controller, Service, Entity, ViewModel, Razor View, and AppDbContext. Business rules, state transitions, file security, and notification logic are mainly handled in the service layer.

The implemented platform includes modules such as user authentication, equipment ledger, qualification review, scheduling, contract management, entry verification, safety briefing, inspection supervision, fault handling, return evaluation, dashboard, notifications, and controlled file access. Test results show that the system can support the main business process of construction equipment rental, ensure expected state transitions, and provide basic permission isolation and file upload security for the graduation design scenario.

Keywords: Construction Equipment Rental; Full-Life-Cycle Management; ASP.NET Core MVC; EF Core; RBAC; SQL Server

Classification: TP311

---
