import path from "node:path";

const C = {
  bg: "#F6F8FB",
  paper: "#FFFFFF",
  ink: "#132033",
  muted: "#64748B",
  soft: "#D9E2EC",
  line: "#CBD5E1",
  navy: "#14213D",
  navy2: "#20304F",
  gold: "#F2B705",
  gold2: "#FFE7A3",
  green: "#15A46E",
  blue: "#2563EB",
  red: "#C2410C",
  dark: "#0B1220",
};

const FONT = {
  title: "PingFang SC",
  body: "PingFang SC",
  mono: "Aptos Mono",
};

function text(slide, ctx, value, x, y, w, h, opts = {}) {
  return ctx.addText(slide, {
    text: String(value ?? ""),
    left: x,
    top: y,
    width: w,
    height: h,
    fontSize: opts.size ?? 18,
    color: opts.color ?? C.ink,
    bold: Boolean(opts.bold),
    typeface: opts.face ?? FONT.body,
    align: opts.align ?? "left",
    valign: opts.valign ?? "top",
    fill: opts.fill ?? "#00000000",
    line: opts.line ?? ctx.line(),
    insets: opts.insets ?? { left: 0, right: 0, top: 0, bottom: 0 },
    name: opts.name,
  });
}

function rect(slide, ctx, x, y, w, h, fill, opts = {}) {
  return ctx.addShape(slide, {
    left: x,
    top: y,
    width: w,
    height: h,
    geometry: opts.geometry ?? "rect",
    fill,
    line: opts.line ?? ctx.line(opts.lineColor ?? "#00000000", opts.lineWidth ?? 0),
    name: opts.name,
  });
}

function line(slide, ctx, x, y, w, color = C.line, weight = 1) {
  rect(slide, ctx, x, y, w, weight, color);
}

function frame(slide, ctx, x, y, w, h, fill = C.paper, border = C.line) {
  rect(slide, ctx, x, y, w, h, fill, { line: ctx.line(border, 1) });
}

function header(slide, ctx, kicker, title, subtitle) {
  rect(slide, ctx, 0, 0, 1280, 720, C.bg);
  rect(slide, ctx, 0, 0, 1280, 8, C.gold);
  text(slide, ctx, kicker, 64, 38, 500, 18, { size: 9.5, bold: true, color: C.gold });
  text(slide, ctx, title, 64, 66, 940, 74, { size: 29, bold: true, color: C.ink, face: FONT.title });
  if (subtitle) text(slide, ctx, subtitle, 64, 148, 900, 30, { size: 12.5, color: C.muted });
}

function footer(slide, ctx, n, note) {
  line(slide, ctx, 64, 674, 1152, C.soft, 1);
  text(slide, ctx, note, 64, 686, 940, 16, { size: 8.5, color: C.muted });
  text(slide, ctx, String(n).padStart(2, "0"), 1170, 682, 46, 22, { size: 12, bold: true, color: C.muted, align: "right", face: FONT.mono });
}

function bulletList(slide, ctx, items, x, y, w, opts = {}) {
  const gap = opts.gap ?? 46;
  items.forEach((item, i) => {
    const yy = y + i * gap;
    rect(slide, ctx, x, yy + 6, 10, 10, item.color ?? C.gold);
    text(slide, ctx, item.title ?? item, x + 24, yy, w - 24, 22, {
      size: opts.titleSize ?? 16,
      bold: true,
      color: item.titleColor ?? C.ink,
    });
    if (item.body) text(slide, ctx, item.body, x + 24, yy + 25, w - 24, 30, { size: opts.bodySize ?? 10.5, color: C.muted });
  });
}

function chip(slide, ctx, value, x, y, w, color = C.navy, fill = C.paper) {
  frame(slide, ctx, x, y, w, 34, fill, color);
  text(slide, ctx, value, x + 10, y + 8, w - 20, 18, { size: 10.5, bold: true, color, align: "center" });
}

function metric(slide, ctx, value, label, x, y, w, color = C.navy) {
  frame(slide, ctx, x, y, w, 96, C.paper, C.soft);
  rect(slide, ctx, x, y, 5, 96, color);
  text(slide, ctx, value, x + 20, y + 16, w - 40, 34, { size: 28, bold: true, color, face: FONT.title });
  text(slide, ctx, label, x + 20, y + 56, w - 40, 20, { size: 11.5, bold: true, color: C.ink });
}

function arrow(slide, ctx, x, y, w, color = C.line) {
  line(slide, ctx, x, y, w, color, 2);
  text(slide, ctx, "›", x + w - 6, y - 15, 20, 28, { size: 24, bold: true, color, align: "center" });
}

async function screenshot(slide, ctx, name, x, y, w, h, label) {
  frame(slide, ctx, x - 4, y - 4, w + 8, h + 8, "#FFFFFF", C.line);
  await ctx.addImage(slide, {
    path: path.join(ctx.assetDir, "screenshots", name),
    left: x,
    top: y,
    width: w,
    height: h,
    fit: "cover",
    alt: label ?? name,
  });
  if (label) {
    rect(slide, ctx, x, y + h - 30, w, 30, "#14213DEE");
    text(slide, ctx, label, x + 14, y + h - 22, w - 28, 16, { size: 9.5, bold: true, color: "#FFFFFF" });
  }
}

function processNode(slide, ctx, label, x, y, w, h, fill = C.paper, accent = C.gold) {
  frame(slide, ctx, x, y, w, h, fill, C.soft);
  rect(slide, ctx, x, y, 5, h, accent);
  text(slide, ctx, label, x + 14, y + 14, w - 28, h - 24, { size: 13, bold: true, color: C.ink, align: "center", valign: "mid" });
}

function layer(slide, ctx, label, body, x, y, w, h, fill, accent) {
  frame(slide, ctx, x, y, w, h, fill, C.soft);
  rect(slide, ctx, x, y, 6, h, accent);
  text(slide, ctx, label, x + 22, y + 14, w - 44, 22, { size: 15, bold: true, color: C.ink });
  text(slide, ctx, body, x + 22, y + 42, w - 44, h - 50, { size: 10.5, color: C.muted });
}

function qaRow(slide, ctx, y, cells, widths, colors = []) {
  let x = 84;
  cells.forEach((cell, i) => {
    frame(slide, ctx, x, y, widths[i], 38, i === 0 ? "#F8FAFC" : "#FFFFFF", C.soft);
    text(slide, ctx, cell, x + 10, y + 11, widths[i] - 20, 18, {
      size: 9.5,
      bold: i === 0,
      color: colors[i] ?? C.ink,
      align: i > 1 ? "center" : "left",
    });
    x += widths[i];
  });
}

function titleCard(slide, ctx, title, body, x, y, w, h, color = C.navy) {
  frame(slide, ctx, x, y, w, h, C.paper, C.soft);
  rect(slide, ctx, x, y, w, 5, color);
  text(slide, ctx, title, x + 18, y + 20, w - 36, 28, { size: 15, bold: true, color });
  text(slide, ctx, body, x + 18, y + 58, w - 36, h - 70, { size: 10.5, color: C.muted });
}

export async function makeSlide(index, presentation, ctx) {
  const slide = presentation.slides.add();

  if (index === 1) {
    rect(slide, ctx, 0, 0, 1280, 720, C.navy);
    rect(slide, ctx, 0, 0, 1280, 10, C.gold);
    rect(slide, ctx, 64, 74, 8, 72, C.gold);
    text(slide, ctx, "毕业设计答辩", 88, 76, 340, 26, { size: 14, bold: true, color: C.gold });
    text(slide, ctx, "基于 ASP.NET Core MVC 的\n建筑租赁设备全生命周期管理平台", 88, 158, 850, 135, { size: 39, bold: true, color: "#FFFFFF", face: FONT.title });
    text(slide, ctx, "EquipmentRental | C# 13 / .NET 10 / SQL Server / MVC", 90, 324, 760, 28, { size: 15, color: "#D6E1EF" });
    metric(slide, ctx, "7", "核心业务阶段", 90, 430, 190, C.gold);
    metric(slide, ctx, "5", "业务角色", 310, 430, 190, C.blue);
    metric(slide, ctx, "15", "E2E 套件完成", 530, 430, 190, C.green);
    frame(slide, ctx, 820, 404, 330, 140, "#FFFFFF12", "#FFFFFF33");
    text(slide, ctx, "作者：Evan Chen\n学院 / 专业：待填写\n学号：待填写\n指导老师：待填写", 846, 432, 280, 86, { size: 14, color: "#FFFFFF" });
    footer(slide, ctx, 1, "讲稿提示：用一句话概括系统对象、技术栈和要解决的行业流程问题。");
    return slide;
  }

  if (index === 2) {
    header(slide, ctx, "BACKGROUND", "研究背景：建筑设备租赁仍高度依赖线下协同", "业务痛点不在单个表单，而在跨角色、跨阶段的状态不可追溯。");
    const items = [
      { title: "纸质台账 / Excel 分散", body: "设备状态、证件有效期和历史租赁记录更新滞后。", color: C.blue },
      { title: "合同与调度线下流转", body: "排期、签署、核验之间缺少强约束，容易出现断点。", color: C.gold },
      { title: "安全记录难追溯", body: "安全交底、巡检、故障处理和退场评价缺少统一链路。", color: C.red },
    ];
    bulletList(slide, ctx, items, 80, 210, 450, { gap: 92, titleSize: 19, bodySize: 13 });
    frame(slide, ctx, 620, 196, 500, 326, C.paper, C.soft);
    text(slide, ctx, "人工流程的典型断点", 652, 224, 300, 24, { size: 17, bold: true, color: C.ink });
    const nodes = [["设备入库", 670, 290], ["合同签署", 850, 290], ["进场核验", 670, 414], ["退场结算", 850, 414]];
    nodes.forEach(([l, x, y], i) => processNode(slide, ctx, l, x, y, 132, 58, "#F8FAFC", i % 2 ? C.gold : C.blue));
    arrow(slide, ctx, 806, 319, 38, C.line);
    arrow(slide, ctx, 806, 443, 38, C.line);
    line(slide, ctx, 736, 352, 1, C.line, 62);
    line(slide, ctx, 916, 352, 1, C.line, 62);
    chip(slide, ctx, "缺少统一状态机与审计留痕", 716, 555, 290, C.red, "#FFF7ED");
    footer(slide, ctx, 2, "讲稿提示：强调项目选题不是做一个 CRUD，而是把租赁生命周期串起来。");
    return slide;
  }

  if (index === 3) {
    header(slide, ctx, "GOALS", "设计目标：把设备租赁流程做成可管、可查、可控的闭环", "系统围绕生命周期、角色权限、流程联动和合规留痕四个目标展开。");
    titleCard(slide, ctx, "全生命周期管理", "覆盖设备入库、资质审核、线上调度、进场核验、安全交底、使用监管、退场评价。", 72, 206, 260, 180, C.blue);
    titleCard(slide, ctx, "角色化协同", "系统管理员、设备管理员、调度员、项目负责人、安全员各司其职。", 362, 206, 260, 180, C.gold);
    titleCard(slide, ctx, "线上状态流转", "合同扫描件上传、核验通过、故障关闭、退场评价都会驱动业务状态变化。", 652, 206, 260, 180, C.green);
    titleCard(slide, ctx, "安全与审计", "CSRF、Authorize、文件白名单、富文本过滤、操作日志保障安全边界。", 942, 206, 260, 180, C.red);
    frame(slide, ctx, 124, 460, 1028, 80, "#F8FAFC", C.soft);
    text(slide, ctx, "核心目标句", 154, 480, 110, 20, { size: 11, bold: true, color: C.gold });
    text(slide, ctx, "让设备从“入库”到“退场”的每一步都有责任角色、业务凭证、状态推进和可追溯记录。", 274, 474, 760, 30, { size: 19, bold: true, color: C.ink });
    footer(slide, ctx, 3, "讲稿提示：这页回答“你到底设计了什么目标”，后面每页都回扣这四点。");
    return slide;
  }

  if (index === 4) {
    header(slide, ctx, "PROCESS", "业务流程总览：七阶段闭环覆盖租赁全生命周期", "状态推进来自业务动作，而不是人工口头确认。");
    const labels = ["设备入库", "资质审核", "线上调度", "进场核验", "安全交底", "使用监管", "退场评价"];
    labels.forEach((l, i) => {
      const x = 74 + i * 165;
      processNode(slide, ctx, l, x, 250, 122, 82, i === 0 ? "#EFF6FF" : C.paper, i % 2 ? C.gold : C.blue);
      if (i < labels.length - 1) arrow(slide, ctx, x + 128, 292, 31, C.line);
      text(slide, ctx, String(i + 1).padStart(2, "0"), x + 42, 202, 40, 28, { size: 20, bold: true, color: i % 2 ? C.gold : C.blue, align: "center", face: FONT.mono });
      line(slide, ctx, x + 61, 230, 1, C.soft, 20);
    });
    const states = [
      "待审核 → 空闲",
      "证件有效才可调度",
      "Unsigned → Signed",
      "Signed → InProgress",
      "Draft → Completed",
      "InUse ↔ Maintenance",
      "Complete → 空闲/维修/报废",
    ];
    states.forEach((s, i) => text(slide, ctx, s, 67 + i * 165, 360, 136, 34, { size: 9.5, color: C.muted, align: "center" }));
    frame(slide, ctx, 178, 462, 924, 70, "#FFFFFF", C.soft);
    text(slide, ctx, "答辩演示路线", 205, 484, 120, 18, { size: 10, bold: true, color: C.gold });
    text(slide, ctx, "登录切换角色 → 新建设备 → 审核通过 → 提交申请 → 排期合同 → 核验交底 → 巡检故障 → 退场评价", 340, 478, 720, 26, { size: 16, bold: true, color: C.ink });
    footer(slide, ctx, 4, "讲稿提示：用这张图建立全场地图，后续核心功能按这条链路展开。");
    return slide;
  }

  if (index === 5) {
    header(slide, ctx, "RBAC", "角色与权限设计：五类用户支撑跨部门协同", "菜单只是体验层，Controller/Action 上的 Authorize 才是最终权限边界。");
    const roles = [
      ["系统管理员", "用户管理、全局查看"],
      ["设备管理员", "设备入库、资质审核、故障处理、退场评价"],
      ["调度员", "用车申请处理、排期、合同与调度日历"],
      ["项目负责人", "提交申请、进场核验、退场申请"],
      ["安全员", "安全交底、巡检、故障上报"],
    ];
    roles.forEach((r, i) => {
      const y = 190 + i * 68;
      frame(slide, ctx, 76, y, 380, 50, C.paper, C.soft);
      rect(slide, ctx, 76, y, 5, 50, i % 2 ? C.gold : C.blue);
      text(slide, ctx, r[0], 96, y + 9, 112, 18, { size: 13, bold: true, color: C.ink });
      text(slide, ctx, r[1], 214, y + 10, 220, 18, { size: 10.5, color: C.muted });
    });
    const modules = ["设备", "资质", "调度", "合同", "核验", "安全", "巡检", "故障", "退场"];
    const matrix = [
      ["●","●","●","●","●","●","●","●","●"],
      ["●","●","","○","","","○","●","●"],
      ["","","●","●","○","","","○","○"],
      ["","","○","○","●","签署","○","上报","申请"],
      ["","","","","","●","●","上报","○"],
    ];
    frame(slide, ctx, 522, 176, 656, 400, C.paper, C.soft);
    modules.forEach((m, i) => text(slide, ctx, m, 644 + i * 55, 204, 44, 18, { size: 9.5, bold: true, color: C.muted, align: "center" }));
    roles.forEach((r, row) => {
      text(slide, ctx, r[0], 548, 246 + row * 54, 86, 18, { size: 9.5, bold: true, color: C.ink });
      matrix[row].forEach((v, col) => text(slide, ctx, v, 644 + col * 55, 246 + row * 54, 44, 18, { size: 10.5, color: v === "●" ? C.green : C.muted, bold: true, align: "center" }));
    });
    text(slide, ctx, "● 主要操作    ○ 只读/参与    空白 无权限", 766, 530, 300, 18, { size: 9.5, color: C.muted });
    footer(slide, ctx, 5, "讲稿提示：突出“谁能做什么”，以及越权访问由后端鉴权拒绝。");
    return slide;
  }

  if (index === 6) {
    header(slide, ctx, "ARCHITECTURE", "系统总体架构：ASP.NET Core MVC 分层 + Service 承载业务规则", "控制器保持薄层，复杂状态机和事务边界集中在服务层。");
    layer(slide, ctx, "表现层 Views", "Razor Views、Bootstrap 5、jQuery、Chart.js、Summernote\n负责页面渲染、表单交互和看板图表。", 94, 188, 1090, 78, "#FFFFFF", C.blue);
    layer(slide, ctx, "Web 层 Controllers", "Account / Equipment / Dispatch / Contract / Verification / Safety / Inspection / Fault / Return\n接收请求、调用服务、返回 View 或 JSON。", 94, 294, 1090, 82, "#FFFFFF", C.gold);
    layer(slide, ctx, "业务层 Services", "EquipmentService、DispatchService、VerificationService、SafetyService、FaultService、ReturnService 等\n封装业务规则、状态流转和事务。", 94, 404, 1090, 88, "#FFFFFF", C.green);
    layer(slide, ctx, "数据与基础设施", "EF Core 10 + SQL Server 2022、ASP.NET Core Identity、BCrypt、Uploads 受控文件存储、QuestPDF、EPPlus。", 94, 522, 1090, 76, "#FFFFFF", C.red);
    arrow(slide, ctx, 618, 272, 40, C.line);
    arrow(slide, ctx, 618, 382, 40, C.line);
    arrow(slide, ctx, 618, 498, 40, C.line);
    footer(slide, ctx, 6, "讲稿提示：说明 MVC 不是简单目录结构，而是把业务复杂度压到 Service 层。");
    return slide;
  }

  if (index === 7) {
    header(slide, ctx, "DATA MODEL", "数据库设计：以 Equipment 与 DispatchOrder 串起核心业务对象", "Code First 建模，主链路对象均可回溯到设备、订单和责任用户。");
    const ents = [
      ["Users / Roles", 74, 192, C.navy],
      ["Equipments", 312, 192, C.blue],
      ["Qualifications\nAuditRecords", 312, 312, C.gold],
      ["DispatchRequests", 560, 192, C.navy],
      ["DispatchOrders", 784, 192, C.green],
      ["Contracts\nEntryVerification", 784, 312, C.blue],
      ["SafetyBriefings\nInspectionRecords", 1000, 192, C.gold],
      ["FaultReports\nReturnEvaluation", 1000, 312, C.red],
      ["Notifications\nOperationLogs", 560, 432, C.muted],
    ];
    ents.forEach(([label, x, y, color]) => processNode(slide, ctx, label, x, y, 162, 74, "#FFFFFF", color));
    arrow(slide, ctx, 238, 229, 66, C.line);
    arrow(slide, ctx, 478, 229, 74, C.line);
    arrow(slide, ctx, 724, 229, 52, C.line);
    arrow(slide, ctx, 946, 229, 46, C.line);
    line(slide, ctx, 393, 270, 1, C.line, 38);
    line(slide, ctx, 865, 270, 1, C.line, 38);
    line(slide, ctx, 1081, 270, 1, C.line, 38);
    arrow(slide, ctx, 640, 420, 0, C.line);
    chip(slide, ctx, "关键约束：合同 Contract 与调度单 DispatchOrder 1:1，核验记录 EntryVerification 与调度单 1:1", 258, 560, 760, C.navy, "#FFFFFF");
    footer(slide, ctx, 7, "讲稿提示：讲清核心实体的关系，不需要逐表解释所有字段。");
    return slide;
  }

  if (index === 8) {
    header(slide, ctx, "FEATURE 01", "设备台账与资质审核：让设备“可租”前先合规", "待审核设备必须完成证件维护和审核，通过后才进入可调度池。");
    await screenshot(slide, ctx, "equipment-list.png", 636, 178, 520, 326, "真实页面：设备台账列表");
    bulletList(slide, ctx, [
      { title: "设备入库", body: "编号唯一、分类选择、图片上传，创建后状态为待审核。", color: C.blue },
      { title: "证件管理", body: "产品合格证、年检报告、保险凭证等证件记录有效期。", color: C.gold },
      { title: "审核流转", body: "通过后 PendingReview → Idle；驳回需要备注并留存审核记录。", color: C.green },
    ], 86, 204, 470, { gap: 86, titleSize: 18, bodySize: 12.5 });
    frame(slide, ctx, 86, 520, 462, 58, "#FFFFFF", C.soft);
    text(slide, ctx, "业务约束", 108, 538, 80, 18, { size: 10, bold: true, color: C.gold });
    text(slide, ctx, "证件过期或未审核设备不会进入调度员的可选列表。", 196, 532, 310, 28, { size: 14, bold: true, color: C.ink });
    footer(slide, ctx, 8, "讲稿提示：把这页作为“入口质量控制”，解释为什么审核先于调度。");
    return slide;
  }

  if (index === 9) {
    header(slide, ctx, "FEATURE 02", "线上调度与合同：排期、合同和设备状态在同一链路联动", "扫描件上传是签署完成的业务凭证，也是订单进入核验前的状态门槛。");
    await screenshot(slide, ctx, "dispatch-orders.png", 78, 188, 450, 278, "真实页面：调度单列表");
    await screenshot(slide, ctx, "contract-detail.png", 760, 188, 410, 278, "真实页面：合同在线预览");
    processNode(slide, ctx, "用车申请", 552, 228, 150, 56, "#FFFFFF", C.blue);
    processNode(slide, ctx, "调度排期", 552, 322, 150, 56, "#FFFFFF", C.gold);
    processNode(slide, ctx, "合同草稿", 552, 416, 150, 56, "#FFFFFF", C.green);
    arrow(slide, ctx, 626, 290, 0, C.line);
    arrow(slide, ctx, 626, 384, 0, C.line);
    frame(slide, ctx, 256, 530, 768, 58, "#FFFFFF", C.soft);
    text(slide, ctx, "关键联动", 282, 548, 84, 18, { size: 10, bold: true, color: C.gold });
    text(slide, ctx, "UploadScanAsync：Contract → Signed、DispatchOrder → Signed、Equipment → InUse", 382, 542, 590, 28, { size: 15, bold: true, color: C.ink, face: FONT.mono });
    footer(slide, ctx, 9, "讲稿提示：强调 PDF 导出不等于签署，上传扫描件才推进状态。");
    return slide;
  }

  if (index === 10) {
    header(slide, ctx, "FEATURE 03", "进场核验与安全交底：现场动作产生可追溯记录", "核验码保证设备、订单、证件状态一致后才允许进场作业。");
    await screenshot(slide, ctx, "verification.png", 78, 184, 486, 300, "真实页面：进场核验");
    await screenshot(slide, ctx, "safety-list.png", 704, 184, 486, 300, "真实页面：安全交底记录");
    processNode(slide, ctx, "已签署订单", 586, 218, 104, 54, "#FFFFFF", C.gold);
    processNode(slide, ctx, "UUID 核验码", 586, 314, 104, 54, "#FFFFFF", C.blue);
    processNode(slide, ctx, "进行中", 586, 410, 104, 54, "#FFFFFF", C.green);
    arrow(slide, ctx, 638, 278, 0, C.line);
    arrow(slide, ctx, 638, 374, 0, C.line);
    frame(slide, ctx, 154, 535, 972, 56, "#FFFFFF", C.soft);
    text(slide, ctx, "安全交底支持富文本、附件、参与人和双方签署；完成后 Draft → Completed，并可导出 PDF。", 190, 552, 880, 20, { size: 14, bold: true, color: C.ink });
    footer(slide, ctx, 10, "讲稿提示：这页解释“为什么核验和交底能形成现场合规证据”。");
    return slide;
  }

  if (index === 11) {
    header(slide, ctx, "FEATURE 04", "巡检故障与退场评价：把使用过程和押金结算纳入闭环", "使用阶段的异常不是孤立记录，会影响设备状态、维修记录和退场评价。");
    await screenshot(slide, ctx, "return-list.png", 702, 186, 488, 306, "真实页面：退场申请列表");
    const flow = [
      ["日常巡检", "8 项固定清单\n异常自动标红", C.blue],
      ["故障上报", "InUse → Maintenance\n工单受理与关闭", C.red],
      ["退场申请", "项目结束后提交\n等待设备管理员评价", C.gold],
      ["押金核算", "扣款服务端校验\n退款金额自动计算", C.green],
    ];
    flow.forEach((f, i) => titleCard(slide, ctx, f[0], f[1], 84 + (i % 2) * 250, 190 + Math.floor(i / 2) * 156, 210, 116, f[2]));
    frame(slide, ctx, 100, 536, 480, 56, "#FFFFFF", C.soft);
    text(slide, ctx, "状态回写", 124, 553, 80, 18, { size: 10, bold: true, color: C.gold });
    text(slide, ctx, "退场评价后订单 Complete，设备回到空闲、维修或报废。", 212, 548, 326, 24, { size: 13.5, bold: true, color: C.ink });
    footer(slide, ctx, 11, "讲稿提示：说明系统不是只管租前，也覆盖租中监管和租后结算。");
    return slide;
  }

  if (index === 12) {
    header(slide, ctx, "SECURITY", "安全与可靠性设计：权限、输入、文件和审计四道边界", "安全设计贯穿登录、表单、富文本、文件下载和业务状态变更。");
    const controls = [
      ["身份认证", "ASP.NET Core Identity + BCrypt 密码哈希，登录失败锁定。", C.blue],
      ["访问控制", "受保护页面显式 Authorize，角色常量集中管理。", C.gold],
      ["CSRF 防护", "全局 AutoValidateAntiforgeryTokenAttribute，AJAX POST 带 token。", C.green],
      ["上传安全", "JPG/JPEG/PNG/PDF，10 MB，扩展名 + MIME + 文件魔数 + GUID 重命名。", C.red],
      ["富文本过滤", "HtmlSanitizer 入库前清洗安全交底内容。", C.navy],
      ["审计留痕", "OperationLogs 记录关键增删改与状态流转。", C.muted],
    ];
    controls.forEach((c, i) => titleCard(slide, ctx, c[0], c[1], 82 + (i % 3) * 370, 194 + Math.floor(i / 3) * 170, 318, 120, c[2]));
    frame(slide, ctx, 212, 558, 856, 44, "#FFFFFF", C.soft);
    text(slide, ctx, "Uploads 不放在 wwwroot，文件通过 FilesController 鉴权下发，避免绕过权限直接访问。", 248, 571, 784, 16, { size: 12.5, bold: true, color: C.ink });
    footer(slide, ctx, 12, "讲稿提示：不要只说“做了登录”，要按攻击面解释每道边界。");
    return slide;
  }

  if (index === 13) {
    header(slide, ctx, "VALIDATION", "测试与验证：主业务链路已经真实浏览器端到端跑通", "QA 记录覆盖角色边界、状态流转、上传校验、控制台错误和回归问题。");
    metric(slide, ctx, "15", "2026-04-22 主链路套件通过", 84, 184, 250, C.green);
    metric(slide, ctx, "0", "P0 阻断问题", 374, 184, 210, C.blue);
    metric(slide, ctx, "5层", "文件上传校验", 624, 184, 210, C.gold);
    metric(slide, ctx, "400", "无 CSRF Token POST 拒绝", 874, 184, 250, C.red);
    const widths = [220, 180, 120, 360];
    qaRow(slide, ctx, 330, ["范围", "结果", "方式", "说明"], widths, [C.ink, C.ink, C.ink, C.ink]);
    [
      ["登录 / 角色边界", "通过", "浏览器", "不同角色菜单与越权访问验证"],
      ["设备入库 → 审核", "通过", "浏览器+落库", "图片、证件、审核状态流转"],
      ["调度 → 合同 → 核验", "通过", "浏览器", "上传扫描件后进入已签署并生成核验记录"],
      ["安全交底 → 巡检 → 故障", "通过", "浏览器", "双签完成、异常巡检、故障关闭"],
      ["退场评价", "通过", "浏览器+服务端", "扣款与押金退款校验"],
    ].forEach((r, i) => qaRow(slide, ctx, 368 + i * 38, r, widths, [C.ink, C.green, C.muted, C.muted]));
    footer(slide, ctx, 13, "讲稿提示：这里给评委信心：系统不是静态页面，主流程已经跑通并记录 QA。");
    return slide;
  }

  if (index === 14) {
    header(slide, ctx, "CONCLUSION", "总结与展望：从流程线上化走向智能化和生态集成", "本项目完成了建筑设备租赁管理的核心闭环，也保留了后续扩展空间。");
    titleCard(slide, ctx, "已完成", "实现 7 阶段生命周期、5 角色权限、合同 PDF、核验码、安全交底、巡检故障、退场评价和首页看板。", 92, 204, 322, 190, C.green);
    titleCard(slide, ctx, "项目价值", "减少线下台账和人工状态确认，让设备、合同、安全与退场记录在一个系统内闭环。", 478, 204, 322, 190, C.blue);
    titleCard(slide, ctx, "后续展望", "可扩展移动端扫码、GPS/IoT 状态采集、财务发票对接、电子签章和更细粒度的数据分析。", 864, 204, 322, 190, C.gold);
    frame(slide, ctx, 196, 480, 888, 70, "#FFFFFF", C.soft);
    text(slide, ctx, "最终结论", 226, 502, 100, 18, { size: 10, bold: true, color: C.gold });
    text(slide, ctx, "系统已形成“设备可查、流程可控、风险可追溯”的工程化实现。", 342, 494, 638, 28, { size: 19, bold: true, color: C.ink });
    text(slide, ctx, "谢谢各位老师，请批评指正", 448, 612, 384, 30, { size: 24, bold: true, color: C.navy, align: "center" });
    footer(slide, ctx, 14, "讲稿提示：收束到工程价值，再给出可落地的后续方向。");
    return slide;
  }

  throw new Error(`Unknown slide index: ${index}`);
}
