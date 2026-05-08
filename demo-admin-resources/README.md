# 单 Admin 演示资源包

本目录用于答辩现场单账号演示。所有资源均围绕 `admin@equiprental.com` 全流程演示准备，可配合 `docs/admin-single-account-demo.md` 使用。

## 文件清单

| 文件 | 用途 | 上传位置 |
|---|---|---|
| `form-data.md` | 页面表单填写数据速查 | 不上传，现场照抄 |
| `demo-equipment.png` | 设备入库图片 | 设备台账新建设备 |
| `demo-qualification.pdf` | 资质证书模拟件 | 设备资质/证件附件 |
| `demo-contract-scan.pdf` | 合同扫描件模拟件 | 合同详情上传扫描件 |

## 演示账号

| 账号 | 密码 |
|---|---|
| `admin@equiprental.com` | `Admin@123456` |

演示前建议退出登录或清理浏览器 Cookie，确保当前浏览器登录态属于本次演示数据库。

## 推荐顺序

1. 登录 Admin，展示首页和模块入口。
2. 使用 `form-data.md` 的设备数据新建设备，并上传 `demo-equipment.png`。
3. 为该设备新增资质证书，上传 `demo-qualification.pdf`。
4. 审核通过设备。
5. 提交用车申请，调度该设备，生成合同。
6. 上传 `demo-contract-scan.pdf`，推进合同到已签署。
7. 完成进场核验、安全交底、巡检、故障处理。
8. 提交退场申请，从详情页点击填写退场评价，完成订单闭环。

## 上传校验说明

系统只允许 `.jpg`、`.jpeg`、`.png`、`.pdf` 文件，并检查 MIME 类型、文件魔数和 10 MB 大小限制。本目录内的 PNG/PDF 已按这些规则生成。
