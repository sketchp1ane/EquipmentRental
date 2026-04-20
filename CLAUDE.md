# EquipmentRental — Claude Code 上下文

## 项目简介
基于 ASP.NET Core MVC 8 的建筑租赁设备全生命周期管理平台。
业务链路：设备入库 → 资质审核 → 线上调度 → 进场核验 → 安全交底 → 使用监管 → 退场评价。

## 技术栈
- C# 12 / .NET 8 / ASP.NET Core MVC
- EF Core 8，Code First，`AppDbContext`
- SQL Server 2022（Docker），连接串在 `appsettings.Development.json`
- ASP.NET Core Identity（RBAC，6 个角色，见下方）
- Bootstrap 5 + jQuery 3 + Chart.js 4 + Summernote（富文本）
- QuestPDF（PDF 导出）、EPPlus 6（Excel 导出）

## NuGet 包（项目初始化时全部安装）

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package QuestPDF
dotnet add package EPPlus --version 6.*
dotnet add package Ganss.Xss           # HtmlSanitizer，富文本过滤
dotnet add package QRCoder              # 二维码生成
dotnet add package Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation  # 开发时热重载 View
```

## 常用命令

```bash
# 数据库（Docker）
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Admin123456!" \
  -p 1433:1433 --name equiprental-db -d \
  mcr.microsoft.com/mssql/server:2022-latest

# EF Core 迁移
dotnet ef migrations add <MigrationName>   # 新建迁移
dotnet ef database update                  # 应用迁移
dotnet ef migrations remove               # 撤销最后一次未应用的迁移

# 运行
dotnet run
dotnet watch run   # 开发时推荐，文件变更自动重载
```

## 分层规则（严格遵守）
- **Controller**：只做请求接收、调用 Service、映射 ViewModel、返回 View/JSON
- **Service**：全部业务逻辑和状态流转在此，注入 `AppDbContext` 直接操作
- **Model/Entities**：只含属性和导航属性，无业务方法
- **ViewModel**：视图专用 DTO，含 `[Required]` 等 DataAnnotations，不暴露 Entity

## 角色常量（统一使用，不要硬编码字符串）

```csharp
public static class Roles
{
    public const string Admin          = "Admin";
    public const string DeviceAdmin    = "DeviceAdmin";
    public const string Dispatcher     = "Dispatcher";
    public const string ProjectLead    = "ProjectLead";
    public const string SafetyOfficer  = "SafetyOfficer";
    public const string Auditor        = "Auditor";
}
```

## 安全约定（每个 Controller/Action 都要检查）
- POST/PUT/DELETE 必须加 `[ValidateAntiForgeryToken]`，表单加 `@Html.AntiForgeryToken()`
- 受保护页面加 `[Authorize(Roles = Roles.Xxx)]`
- 禁止拼接 SQL，全部走 EF Core LINQ
- 文件上传：校验扩展名白名单（`.jpg .png .pdf`）+ MIME 类型，保存到 `Uploads/`（不在 `wwwroot`）
- 富文本入库前必须经过 `HtmlSanitizer.Sanitize()`

## MCP 工具使用规范

### Context7（文档查询）
查询任何库、框架、SDK、API、CLI 工具的最新文档时必须使用 Context7，即使是熟知的库也不例外（训练数据可能过时）。

**触发场景**：API 语法、配置选项、版本迁移、"如何使用 X 库"、库相关 bug 排查、安装配置。

**步骤**：
1. `mcp__context7__resolve-library-id` — 用库名 + 问题搜索，选最佳匹Match（ID 格式 `/org/project`）
2. `mcp__context7__query-docs` — 用选定 ID + 完整问题查询文档
3. 依据返回文档回答，不依赖训练数据中的 API 细节

**不使用场景**：重构、从零写脚本、业务逻辑 debug、代码审查、通用编程概念。

### Playwright MCP（浏览器自动化 & UI 测试）
实现前端功能后，**必须**启动开发服务器并用 Playwright MCP 在浏览器中验证，而不是仅依赖类型检查或测试套件。

**常用工具**：
- `mcp__playwright__browser_navigate` — 打开页面
- `mcp__playwright__browser_snapshot` — 获取页面结构（用于定位元素）
- `mcp__playwright__browser_take_screenshot` — 截图确认视觉效果
- `mcp__playwright__browser_click` / `browser_fill_form` / `browser_type` — 交互操作
- `mcp__playwright__browser_wait_for` — 等待异步内容加载
- `mcp__playwright__browser_console_messages` — 捕获 JS 错误

**验证要求**：UI 任务完成后必须测试黄金路径 + 关键边界，截图作为完成证明，发现 JS 错误需立即排查。

## 参考文档
- `docs/PRD.md` — 完整功能需求、角色权限矩阵、业务流程
- `docs/database.md` — 所有表结构、字段类型、枚举值、索引
- `docs/architecture.md` — 目录结构、分层职责、模块依赖
- `docs/progress.md` — 当前开发进度（每次新 session 先看这里）
