# 06 权限和安全设计

## 登录和密码

登录体系在 [../../Program.cs](../../Program.cs) 配置：

| 配置 | 含义 |
|---|---|
| `AddIdentity<ApplicationUser, IdentityRole>` | 使用 ASP.NET Core Identity |
| 密码长度 8，要求大小写和数字 | 基础密码强度 |
| 失败 5 次锁定 30 分钟 | 防暴力破解 |
| `BCryptPasswordHasher` | 用 BCrypt 替换默认哈希 |
| Cookie 30 分钟滑动过期 | 登录态管理 |

账号实体是 [../../Models/Entities/ApplicationUser.cs](../../Models/Entities/ApplicationUser.cs)，角色常量在 [../../Constants/Roles.cs](../../Constants/Roles.cs)。

## 角色授权

授权主要靠 Controller 上的 `[Authorize]`：

| 位置 | 作用 |
|---|---|
| 类级 `[Authorize]` | 整个 Controller 需要登录 |
| Action 级 `[Authorize(Roles = ...)]` | 某个操作限制角色 |
| `Constants/Roles.cs` | 统一角色名，避免硬编码 |

例子：

```text
EquipmentController
  -> Admin / DeviceAdmin / Auditor 可看列表和详情
  -> Admin / DeviceAdmin 可新增、编辑、删图片

AuditController
  -> Admin / DeviceAdmin 才能审核

VerificationController.Verify
  -> ProjectLead / Admin 才能执行核验
```

## CSRF 防护

[../../Program.cs](../../Program.cs) 全局注册了：

```text
AutoValidateAntiforgeryTokenAttribute
```

含义：

- 标准 Razor 表单会自动带 antiforgery token。
- POST/PUT/DELETE 等修改请求会验证 token。
- 手写 AJAX POST 时要特别注意带 token。

## 文件上传安全

上传保存逻辑在 [../../Services/FileService.cs](../../Services/FileService.cs)：

| 防护 | 实现 |
|---|---|
| 扩展名白名单 | 只允许 `.jpg`、`.jpeg`、`.png`、`.pdf` |
| 文件大小限制 | 最大 10 MB |
| MIME 白名单 | Content-Type 必须匹配扩展名 |
| 魔数校验 | 读取文件头确认真实类型 |
| GUID 重命名 | 避免路径穿越和重名覆盖 |
| 存储位置 | 保存到 `Uploads/`，不放 `wwwroot/` |

文件访问入口在 [../../Controllers/FilesController.cs](../../Controllers/FilesController.cs)，通过 `/files/{*filePath}` 鉴权下发，并检查解析后的路径必须仍在 `Uploads/` 下。

## 富文本安全

安全交底支持富文本，服务注册里有：

```text
AddScoped<Ganss.Xss.HtmlSanitizer>()
```

读 [../../Services/SafetyService.cs](../../Services/SafetyService.cs) 时重点看富文本内容保存前如何经过 Sanitizer，避免把危险 HTML 直接持久化。

## 审计和通知

多处 Service 会写：

| 机制 | 作用 |
|---|---|
| `OperationLog` | 记录谁在什么时间执行了关键操作 |
| `NotificationService.SendAsync` | 给相关角色发送站内消息 |

常见触发点：

- 资质审核。
- 调度生成。
- 进场核验通过或失败记录。
- 故障上报和关闭。
- 退场评价。

## 权限问题排查顺序

1. 看 Controller 类和 Action 上的 `[Authorize]`。
2. 看是否使用了 `Constants.Roles`。
3. 看侧边栏或按钮是否也按角色隐藏。
4. 看 POST 是否被 CSRF 拦截。
5. 看 Service 是否还有业务状态校验，因为 UI 权限通过不代表业务一定允许。
