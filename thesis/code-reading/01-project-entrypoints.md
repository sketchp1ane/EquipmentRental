# 01 项目入口文件

## 从 `Program.cs` 开始

[../../Program.cs](../../Program.cs) 是整个应用的启动入口。读它时按这个顺序看：

| 区块 | 代码含义 |
|---|---|
| QuestPDF / EPPlus License | PDF 和 Excel 导出库的运行配置 |
| `AddDbContext<AppDbContext>` | 使用 SQL Server，连接串来自配置文件 |
| `AddIdentity<ApplicationUser, IdentityRole>` | 登录、角色、锁定、密码规则 |
| `AddScoped<IPasswordHasher<...>, BCryptPasswordHasher>` | 用 BCrypt 替换 Identity 默认密码哈希 |
| `ConfigureApplicationCookie` | 登录页、退出页、403 页、Cookie 过期策略 |
| `AddControllersWithViews` | 启用 MVC 和全局 CSRF 验证 |
| `AddScoped<...Service>` | 注册业务服务 |
| `UseAuthentication` / `UseAuthorization` | 启用认证和授权中间件 |
| `MapControllerRoute` | 默认路由 `{controller=Home}/{action=Index}/{id?}` |
| `MigrateAsync` / `DbInitializer.SeedAsync` | 启动时迁移数据库并写入种子数据 |

## 配置文件

| 文件 | 作用 |
|---|---|
| [../../EquipmentRental.csproj](../../EquipmentRental.csproj) | TargetFramework 是 `net10.0`，依赖 EF Core、Identity、QuestPDF、EPPlus、QRCoder 等 |
| [../../appsettings.json](../../appsettings.json) | 通用配置 |
| [../../appsettings.Development.json](../../appsettings.Development.json) | 本地数据库连接串 |
| [../../Properties/launchSettings.json](../../Properties/launchSettings.json) | 本地启动地址，当前 HTTP 是 `http://localhost:5085` |
| [../../libman.json](../../libman.json) | Bootstrap、jQuery、Chart.js 等前端库 |

## 服务注册清单

当前注册的业务服务都在 `Program.cs` 里用 `AddScoped` 注入：

```text
EquipmentService
QualificationService
AuditService
DispatchService
VerificationService
SafetyService
InspectionService
FaultService
ReturnService
NotificationService
DashboardService
FileService
UserService
HtmlSanitizer
```

源码阅读时，如果 Controller 构造函数里注入了某个服务，就回到这里确认它是如何被 DI 容器提供的。

## 数据库初始化入口

[../../Data/DbInitializer.cs](../../Data/DbInitializer.cs) 用于写入：

- 六类角色。
- 管理员和演示账号。
- 设备分类、演示设备、证件、调度单、合同、巡检、故障、退场等示例数据。

读业务流程前建议先看种子数据，因为演示页面里的很多记录就是从这里来的。

## 启动后的第一条请求

```text
浏览器访问 /
  -> 默认路由
  -> HomeController.Index
  -> DashboardService.GetDashboardStatsAsync
  -> Views/Home/Index.cshtml
```

这条链路适合拿来理解 MVC 请求最短路径。
