# 附录A 核心代码

> 正式论文建议只放 3—5 个核心代码片段，不要大段粘贴全部源码。可选择以下文件中的关键方法，并配合简短说明。

## A.1 Program.cs 应用启动与服务注册

建议选取内容：

```text
1. AddDbContext<AppDbContext>() 与 UseSqlServer()
2. AddIdentity<ApplicationUser, IdentityRole>()
3. AddControllersWithViews() 与 AutoValidateAntiforgeryTokenAttribute
4. AddScoped<EquipmentService>()、AddScoped<DispatchService>() 等服务注册
5. MigrateAsync() 与 DbInitializer.SeedAsync()
```

获取方法：

```text
从项目根目录 Program.cs 复制关键代码片段，删去与论文无关的注释和重复服务注册，只保留能证明架构、认证、数据库和服务注册的部分。
```

## A.2 DispatchService 调度与合同状态推进

建议选取内容：

```text
1. 可用设备筛选方法
2. 创建调度单方法
3. 上传合同扫描件并推进合同和调度单状态的方法
```

获取方法：

```text
从 Services/DispatchService.cs 中截取 GetAvailableEquipmentsAsync、CreateOrderAsync、UploadScanAsync 等核心方法。
```

## A.3 VerificationService 进场核验

建议选取内容：

```text
1. 校验调度单状态
2. 校验核验码
3. 生成 EntryVerification
4. 更新调度单和设备状态
```

获取方法：

```text
从 Services/VerificationService.cs 中截取 PerformVerifyAsync 的核心逻辑。
```

## A.4 FileService 文件上传安全校验

建议选取内容：

```text
1. 文件大小校验
2. 扩展名白名单
3. MIME 类型校验
4. 文件魔数校验
5. GUID 重命名
6. 保存到 Uploads 目录
```

获取方法：

```text
从 Services/FileService.cs 中截取上传校验和保存逻辑。
```

---

# 附录B 系统运行截图

> 正式论文建议截图放在附录，不要在正文插入过多页面截图。截图应清晰，尺寸统一，图题按“图B.1”等编号。

建议截图清单：

```text
图B.1 系统登录页面
图B.2 首页看板页面
图B.3 设备台账列表页面
图B.4 设备详情与资质证件页面
图B.5 调度排期页面
图B.6 合同预览页面
图B.7 进场核验页面
图B.8 安全交底页面
图B.9 故障工单处理页面
图B.10 退场评价页面
```

获取方法：

```text
1. 启动项目，访问 http://localhost:5085。
2. 使用不同角色账号登录系统。
3. 用浏览器截图工具或系统截图工具截取关键页面。
4. 截图前清理无关浏览器标签和桌面内容。
5. 图片建议统一宽度，插入 Word 后保持清晰。
6. 附录截图只保留最能说明系统实现的页面，避免过多截图占用篇幅。
```

---
