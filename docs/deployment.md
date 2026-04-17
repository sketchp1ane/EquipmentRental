# 部署指南

## 目录

1. [环境要求](#1-环境要求)
2. [开发环境搭建](#2-开发环境搭建)
3. [生产环境部署](#3-生产环境部署)
4. [常见问题](#4-常见问题)

---

## 1. 环境要求

| 依赖 | 最低版本 | 说明 |
|---|---|---|
| .NET SDK | 10.0 | `dotnet --version` 确认 |
| Docker Desktop | 20.10 | 用于运行 SQL Server |
| SQL Server 镜像 | 2022-latest | 首次拉取约 1.5 GB |

> macOS/Linux 不支持原生安装 SQL Server，**必须通过 Docker 运行**。

---

## 2. 开发环境搭建

### 2.1 拉取并启动 SQL Server

```bash
docker run \
  -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=Admin123456!" \
  -p 1433:1433 \
  --name equiprental-db \
  --restart unless-stopped \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

验证容器正在运行：

```bash
docker ps | grep equiprental-db
```

### 2.2 配置本地连接串

在项目根目录（与 `appsettings.json` 同级）创建 `appsettings.Development.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=EquipmentRentalDb;User Id=sa;Password=Admin123456!;TrustServerCertificate=True;MultipleActiveResultSets=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

> `appsettings.Development.json` 已在 `.gitignore` 中排除，不会提交到版本库。

### 2.3 安装 EF Core 工具（首次）

```bash
dotnet tool install --global dotnet-ef
```

### 2.4 应用数据库迁移

```bash
dotnet ef database update
```

迁移成功后，程序首次启动会自动执行种子数据：
- 创建 6 个系统角色（Admin / DeviceAdmin / Dispatcher / ProjectLead / SafetyOfficer / Auditor）
- 创建默认管理员账号（`admin@equiprental.com` / `Admin@123456`）
- 预置 8 大类 + 27 个二级设备分类

### 2.5 创建上传目录

项目根目录下的 `Uploads/` 文件夹在首次文件上传时会自动创建，无需手动操作。若需提前创建：

```bash
mkdir -p Uploads
```

> `Uploads/` 存储在 `wwwroot` 之外，不会被静态文件中间件直接暴露。文件通过控制器 Action 鉴权后下载。

### 2.6 启动应用

```bash
# 普通启动
dotnet run

# 开发模式（推荐，文件变更自动重载）
dotnet watch run
```

默认监听 `http://localhost:5000`，浏览器访问后跳转到登录页。

---

## 3. 生产环境部署

> 本项目为毕业设计，以下为标准化部署参考步骤。

### 3.1 发布应用

```bash
dotnet publish -c Release -o ./publish
```

### 3.2 生产配置

在发布目录创建 `appsettings.Production.json`，填写生产数据库连接串，并将密码替换为强密码：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<DB_HOST>,1433;Database=EquipmentRentalDb;User Id=sa;Password=<STRONG_PASSWORD>;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

### 3.3 强制 HTTPS

在 `Program.cs` 中，生产环境已启用 `UseHttpsRedirection()`。需在服务器（如 Nginx 反向代理 / IIS / Kestrel）上配置 SSL 证书。

Nginx 反向代理示例：

```nginx
server {
    listen 443 ssl;
    server_name your-domain.com;

    ssl_certificate     /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location / {
        proxy_pass         http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```

### 3.4 Uploads 目录持久化

确保 `Uploads/` 目录对应用进程可读写，且在容器/服务重启后数据不丢失（挂载持久卷或使用宿主机目录）。

### 3.5 运行发布版

```bash
cd ./publish
dotnet EquipmentRental.dll
```

---

## 4. 常见问题

### 连接数据库失败（`Connection refused` 或超时）

1. 确认 Docker 容器正在运行：`docker ps`
2. 确认端口映射正确：`docker port equiprental-db`
3. 确认 `appsettings.Development.json` 存在且连接串正确
4. 等待约 10 秒让 SQL Server 完全启动后再重试

### 迁移失败（`No migrations to apply` 或表已存在）

```bash
# 检查当前迁移状态
dotnet ef migrations list

# 若数据库有残留，重置（仅开发环境）
dotnet ef database drop --force
dotnet ef database update
```

### 文件上传报错（`DirectoryNotFoundException`）

手动创建 `Uploads/` 目录：

```bash
mkdir -p Uploads
```

### 容器重启后数据库数据丢失

默认 Docker run 不挂载持久卷。如需保留数据：

```bash
docker run \
  -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=Admin123456!" \
  -p 1433:1433 \
  --name equiprental-db \
  -v equiprental-data:/var/opt/mssql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### 重置管理员密码

直接修改 `DbInitializer.cs` 中 `adminPassword` 常量，然后执行 `dotnet run` 即可（启动时自动更新）。
