# 02 MVC 分层怎么读

## 分层职责

| 层 | 目录 | 主要职责 | 阅读重点 |
|---|---|---|---|
| Controller | [../../Controllers](../../Controllers) | 接收请求、角色授权、ModelState 验证、调用 Service、返回 View/Redirect/File/JSON | Action 名、路由、`[Authorize]`、调用了哪个服务 |
| Service | [../../Services](../../Services) | 业务校验、状态流转、事务、通知、审计日志、DbContext 查询 | 状态何时改变、失败原因、保存了哪些实体 |
| Entity | [../../Models/Entities](../../Models/Entities) | 数据库表字段和导航属性 | 主外键关系、状态字段、关联集合 |
| ViewModel | [../../Models/ViewModels](../../Models/ViewModels) | 页面输入输出 DTO、表单验证属性 | 表单字段、列表字段、详情页字段 |
| Razor View | [../../Views](../../Views) | 页面展示、表单提交、局部视图复用 | `asp-action`、`asp-controller`、按钮显示条件 |
| DbContext | [../../Data/AppDbContext.cs](../../Data/AppDbContext.cs) | `DbSet`、关系配置、索引、删除行为 | 实体关系和唯一约束 |

## 一次标准表单请求

```text
Razor View 表单
  -> Controller POST Action 接收 ViewModel
  -> ModelState.IsValid 做基础验证
  -> Service 做业务验证和状态流转
  -> AppDbContext.SaveChangesAsync
  -> RedirectToAction 或返回原 View 展示错误
```

## Controller 应该怎么看

先看三个东西：

1. 类上的 `[Authorize]`：这个模块是否需要登录。
2. Action 上的 `[Authorize(Roles = ...)]`：谁能访问这个操作。
3. Action 内调用的 Service 方法：真正业务逻辑在哪里。

示例：

```text
DispatchController.Order POST
  -> 只允许 Dispatcher/Admin
  -> 接收 CreateOrderViewModel
  -> 调 DispatchService.CreateOrderAsync
```

## Service 应该怎么看

Service 是最重要的阅读层。重点找：

| 你看到的代码 | 代表什么 |
|---|---|
| `if (...) return (false, "...")` | 业务规则或失败场景 |
| `Include(...)` | 页面需要展示或判断的关联数据 |
| `Status = ...` | 状态流转 |
| `BeginTransactionAsync` | 多实体必须一起成功 |
| `notificationService.SendAsync` | 触发站内消息 |
| `OperationLog` | 记录审计日志 |

## ViewModel 和 View 的关系

如果你要看一个页面字段怎么来的：

```text
Views/Equipment/Create.cshtml
  -> 使用 CreateEquipmentViewModel
  -> EquipmentController.Create GET 准备下拉框等数据
  -> EquipmentController.Create POST 接收表单
  -> EquipmentService.CreateEquipmentAsync 保存实体
```

## Shared 视图组件

[../../Views/Shared](../../Views/Shared) 里有很多复用 UI：

| 文件 | 作用 |
|---|---|
| `_Layout.cshtml` | 主布局 |
| `_SidebarNav.cshtml` | 侧边导航 |
| `_StatusBadge.cshtml` | 状态标签 |
| `_Pagination.cshtml` | 分页 |
| `_AttachmentList.cshtml` | 附件列表 |
| `_FlashToast.cshtml` | 成功/错误提示 |
| `_ConfirmModal.cshtml` | 确认弹窗 |

页面样式或共用组件问题，优先从这里找。
