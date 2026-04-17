# EquipmentRental — 建筑租赁设备管理平台

基于 ASP.NET Core MVC 10 的建筑租赁设备全生命周期管理系统，覆盖设备入库、资质审核、线上调度、进场核验、安全交底、使用监管、退场评价的完整业务链路。

## 环境要求

| 依赖 | 版本 |
|---|---|
| .NET SDK | 10.0+ |
| Docker | 20.10+ |
| SQL Server（Docker 镜像） | 2022 |

## 快速启动

### 1. 启动数据库

```bash
docker run \
  -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=Admin123456!" \
  -p 1433:1433 \
  --name equiprental-db \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### 2. 配置本地连接串

在项目根目录创建 `appsettings.Development.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=EquipmentRentalDb;User Id=sa;Password=Admin123456!;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

### 3. 应用数据库迁移

```bash
dotnet ef database update
```

首次启动会自动执行种子数据（角色、管理员账号、设备分类）。

### 4. 运行

```bash
dotnet run
# 或开发模式（文件变更自动重载）
dotnet watch run
```

默认监听地址：`http://localhost:5000`

## 默认账号

| 账号 | 密码 | 角色 |
|---|---|---|
| admin@equiprental.com | Admin@123456 | 系统管理员 |

登录后可在"用户管理"页面创建其他角色账号。

## 目录结构

```
EquipmentRental/
├── Controllers/     # MVC 控制器（薄层，只做请求路由）
├── Services/        # 业务逻辑层
├── Models/
│   ├── Entities/    # EF Core 实体（与数据库表对应）
│   └── ViewModels/  # 视图专用 DTO
├── Data/            # AppDbContext + 迁移文件 + 种子数据
├── Views/           # Razor 视图（.cshtml）
├── wwwroot/         # 静态资源（Bootstrap / jQuery / Chart.js）
├── Uploads/         # 上传文件存储（不对外暴露）
└── docs/            # 项目文档
```

## 文档

| 文档 | 说明 |
|---|---|
| [docs/PRD.md](docs/PRD.md) | 产品需求文档（功能需求、角色权限矩阵） |
| [docs/database.md](docs/database.md) | 数据库表结构与字段说明 |
| [docs/architecture.md](docs/architecture.md) | 系统架构与分层职责 |
| [docs/deployment.md](docs/deployment.md) | 完整部署指南 |
| [docs/user-guide.md](docs/user-guide.md) | 用户操作手册（按角色） |
| [docs/progress.md](docs/progress.md) | 开发进度 |

## 技术栈

- **后端**：C# / .NET 10 / ASP.NET Core MVC / EF Core 10
- **数据库**：SQL Server 2022
- **前端**：Bootstrap 5 / jQuery 3 / Chart.js 4 / Summernote
- **认证**：ASP.NET Core Identity（RBAC，6 个角色）
- **PDF 导出**：QuestPDF
- **Excel 导出**：EPPlus 6
- **安全**：HtmlSanitizer / CSRF Token / 文件类型白名单
