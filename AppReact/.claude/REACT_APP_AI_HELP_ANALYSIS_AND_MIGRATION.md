# AppReact/.claude 帮助文件分析与迁移方案

## 一、帮助文件总结（它们都做些什么）

### 1. `.claude/commands/` — 命令（Cursor/Claude 可调用的工作流）

| 文件 | 作用 |
|------|------|
| **migrate.md** | **迁移命令**：按项目标准把 AngularJS 功能迁移到 React。引导 AI 读 skill + reference，执行 6 步工作流（定位 Angular 实现 → 理解业务与 DTO → 规划 React → 按 UI 标准实现 → 验证功能与无错误）。需传入 RouteCode 或组件路径。 |
| **analyze-migration.md** | **分析命令**：对比 React 实现与 AngularJS 版本，找出缺失功能、不完整实现、UI 不合规、代码质量问题，输出分析报告；**等用户确认后再改**。参数：`<react-component-path> <angularjs-route-code>`。 |
| **fix-ui.md** | **修 UI 命令**：按项目 UI 标准修复单个 React 组件的布局、主题、按钮、表单、Flexbox、图标等，并引用 ReferenceComponents。参数：组件路径。 |

三者都依赖 `.claude/skills/` 和 `.claude/reference/` 下的文档，路径写死为 `.claude/...`。

---

### 2. `.claude/reference/` — 参考文档库（给 AI 和开发者用）

#### 入口与索引

- **MainPrompt.md**：总索引与“如何用”。说明本项目是 AngularJS → React+Redux+Tailwind+Wijmo 的迁移；列出目录结构、快速参考表、按类别说明各 prompt；约定“先读 MainPrompt，再按任务选场景/具体文档”。也写明了核心原则（对照 Angular、保留业务逻辑、用 theme、用 appHelper.debugLog 等）。
- **UIMainPrompt.md**（在 03-ui/）：UI 文档的主入口，列出所有 UI 标准与布局文档的索引。

#### 00-scenarios/

- **ScenarioTemplates.md**：与 MainPrompt 配合使用的**场景模板**（如：完整功能迁移、分析不完整实现、在现有页加功能、优化 UI、修 bug、重构、新组件、自定义）。每个场景有固定工作流和完成标准。

#### 01-reference-guides/

- **AppProjectFileReferenceGuide.md**：AngularJS 与后端在 solution 中的位置：Controller/Service/Route、MVC/Views/WebAPI、BL、DTO、React 应用路径等；用于“找 Angular 源码、理解结构”。

#### 02-migration/

- **ConverterAngularJsPage.md**：AngularJS 页面转 React 的**分步工作流**（约 9 步）、UI 转换规则（Wijmo、Font Awesome、Tailwind 等）、组件模式。
- **ConverterAngularServiceToReact.md**：**服务转换规则**（方法名保留、URL 查询参数、HTTP 方法、错误处理、类型、检查清单与示例）。
- **DevelopmentPrompt.md**：通用开发指引（参考实现、迁移策略、数据加载与状态管理、最佳实践与调试）。

#### 03-ui/

- **layout/**：布局模式  
  - TailwindFlexBoxRemainSpace.md：剩余空间用 `w-1 flex-auto` / `h-1 flex-auto`，禁止 `flex-1`。  
  - ApplicationConfigurationMenu.md：应用配置菜单实现方式。
- **standards/**：UI 标准与规范  
  - PageLayoutStandards、ButtonStandards、FormStandards、ThemeUsageStandards、TailwindCSSStandards、ReferenceComponents  
  - ContextMenuStandards、ModalPopupStandards、TwoLevelDropdownStandards  
  - Wijmo 相关：WijmoGridMultiSelect、WijmoGridNullRefFixes、WijmoFlexGridContainer、WijmoFlexGridColumnVisibility、WijmoComboBoxCascading、WijmoCollectionViewStandards  
  - DesignPanel_DragAndDropStandards、ValidationMessagesRule、JsonEditingStandards  

#### 04-architecture/

- **ReactCshtmlCachingSolution.md**：React SPA 与 CSHTML 服务端缓存的关系，说明 React 不依赖服务端缓存、DynamicLayoutController 等。

#### 05-Specific-Component-Rule/

- 具体组件/问题的规则：DEBUG_LOG_USAGE、FormMasterDetailFlow、FIX_DRAG_DROP_PLACEHOLDER_BUG、DRAG_TO_BOUNDARY_IMPLEMENTATION_PLAN 等。

#### 其它

- **ProjectMgt.md**、**newComponentCreation.md**、**CompanySecuritySetting-Angular-FileMap.md**、**sysdefineCommandList.txt** 等；**archive/** 为历史/备份文档。

整体作用：为“迁移 + UI + 架构”提供统一术语、工作流和检查清单，保证 React 与 Angular 行为一致且 UI 统一。

---

### 3. `.claude/skills/` — 技能（Agent 在迁移/UI 任务时主动调用的知识包）

| 文件 | 作用 |
|------|------|
| **angularjs-to-react-migration.md** | **迁移技能**：浓缩版迁移规则。包含项目上下文、Angular/后端路径、服务转换（方法名、query、POST、类型、类结构）、UI 规则（flex、Font Awesome、theme、页面布局、按钮、表单）、Wijmo（ComboBox、FlexGrid cell.item、ref、spacer 列）、字典 API、服务端 model vs API、可调面板/弹窗、参考组件、枚举 useEnumValues、状态模式、debugLog、JSON 美化、迁移工作流与质量标准。 |
| **context-menu.md** | **上下文菜单技能**：在列表/网格管理页统一“Context Menu”模式（参考 DataModelDesign）。约定首列 Actions、浮动菜单结构、图标、无右侧按钮列、点击外部关闭等；详细见 ContextMenuStandards.md。 |

---

### 4. `.claude/settings.local.json`

- 仅配置**权限**（如允许 Bash 执行 npm、node、npx tsc、dir、cd、findstr 等）。与“文档内容”无关，属于环境/安全配置。

---

## 二、迁移到 Solution 根目录 .claude 的可行性分析

### 2.1 是否“可以”迁移

- **可以迁移**。这些文件都是 Markdown/JSON，不依赖构建或运行时路径；只要：
  - 在根目录 `.claude` 下用**单独子目录**存放“仅给 React 应用用的”内容，与今后其它 AI 帮助（如 .NET、通用规范）分开；
  - 迁移后**统一更新内部引用路径**（见下）。

### 2.2 推荐目录结构（在 Solution 根目录下）

在根目录已有 `.claude/`（目前仅有 `settings.local.json`）的前提下，建议：

- 把 **React 应用专属** 的 AI 帮助集中到一个子目录，并明确命名，例如：

```
AppAI/
├── .claude/
│   ├── settings.local.json                    # 根项目已有，保留
│   └── react-app/                             # 仅给 React 应用用的 AI 帮助
│       ├── README.md                          # 说明：本目录为 React 迁移/UI 文档，见 CLAUDE.md
│       ├── commands/
│       │   ├── migrate.md
│       │   ├── analyze-migration.md
│       │   └── fix-ui.md
│       ├── reference/
│       │   ├── MainPrompt.md
│       │   ├── 00-scenarios/
│       │   ├── 01-reference-guides/
│       │   ├── 02-migration/
│       │   ├── 03-ui/
│       │   ├── 04-architecture/
│       │   ├── 05-Specific-Component-Rule/
│       │   ├── archive/
│       │   └── ... (其余 reference 文件)
│       └── skills/
│           ├── angularjs-to-react-migration.md
│           └── context-menu.md
└── PlmApplication/
    └── AppReact/
        └── .claude/
            └── (可选) 保留 settings.local.json 或改为指向根 .claude/react-app 的说明
```

这样：

- **根目录 .claude**：今后可增加 `dotnet/`、`general/` 等，与 `react-app/` 并列，互不干扰。
- **标明“给 React 用”**：通过目录名 `react-app` 和其内 `README.md`（以及根目录 CLAUDE.md 中一句说明）即可。

### 2.3 必须做的路径与引用更新

当前文档里大量使用**相对路径**，例如：

- `.claude/skills/...`
- `.claude/reference/02-migration/...`
- `.claude/reference/03-ui/UIMainPrompt.md`

迁移到根目录后，这些路径若继续按“从项目根”解析，应改为（示例）：

- `.claude/react-app/skills/...`
- `.claude/react-app/reference/02-migration/...`
- `.claude/react-app/reference/03-ui/UIMainPrompt.md`

需要批量查找替换的文件类型：

- **commands/** 下 3 个 `.md`：所有 `.claude/` → `.claude/react-app/`
- **skills/** 下 2 个 `.md`：同上
- **reference/** 下所有引用到 `.claude/` 或 `Prompt/` 的 `.md`（如 MainPrompt.md、UIMainPrompt.md、ScenarioTemplates、各 standards 之间的互相引用等）

建议：用“从项目根目录解析”的约定，统一改为 `.claude/react-app/...`；若希望“在 AppReact 目录下也能用”，可在 AppReact 内留一个简短说明或符号链接指向根 `.claude/react-app`（按你们是否常从 AppReact 子目录打开项目而定）。

### 2.4 路径与“工作目录”的约定

- **Cursor/Claude 的工作区**一般是 **Solution 根目录**（AppAI），因此根目录下的 `.claude/react-app/...` 对 AI 是自然路径。
- 文档内写的 **AngularJS/后端路径**（如 `PlmApplication\Scripts1x\mgtCtrl\`、`APP.BL\`）本身是相对 solution 根的，无需因“帮助文件迁移”而改；只需把 **React 应用路径** 统一成当前 solution 结构（例如 `PlmApplication/AppReact/`），在 AppProjectFileReferenceGuide、MainPrompt 的 “Related Resources” 等处检查一遍即可。

### 2.5 settings.local.json 的处理

- **AppReact/.claude/settings.local.json**：包含 npm/node/npx 等权限，适合前端开发。
- **根 .claude/settings.local.json**：当前是 git 等权限。

建议：

- **迁移时**：不把 AppReact 的 `settings.local.json` 覆盖到根，而是**合并**根目录的 permissions（若工具支持合并），或保留两处：根目录侧重通用/git，AppReact 下可保留一份仅在前端上下文中生效的配置（若 Cursor 会按工作区子目录读取）。
- 若只保留根目录一份：把 npm/node/npx 等 allow 项合并进根 `.claude/settings.local.json` 即可。

### 2.6 根目录 CLAUDE.md 的补充说明

在根目录 **CLAUDE.md** 中已有 React 与迁移的简要说明，迁移完成后建议加一句，例如：

- “React 迁移与 UI 的详细 AI 帮助在 `.claude/react-app/`：入口为 `reference/MainPrompt.md`，命令在 `commands/`，技能在 `skills/`。”

这样 AI 和开发者都知道“React 专用帮助”在根目录下的位置，且与“其它 AI 帮助”分开。

---

## 三、迁移执行清单（若采纳本方案）

1. 在根目录创建 `.claude/react-app/`，子目录 `commands/`、`reference/`、`skills/`。
2. 将 `PlmApplication/AppReact/.claude/` 下对应文件**复制**到 `.claude/react-app/`（保留 reference 的目录结构及 archive）。
3. 在 `.claude/react-app/` 内全局替换：`.claude/` → `.claude/react-app/`（仅限指向本帮助库的链接）；若有 `Prompt/` 则改为 `.claude/react-app/reference/`。
4. 检查并更新 MainPrompt.md、AppProjectFileReferenceGuide 等中的 “React App Path”“Solution Root” 为当前 repo（如 `AppAI`）路径。
5. 在 `.claude/react-app/README.md` 写一句：本目录为 React 应用（AppReact）迁移与 UI 的 AI 帮助，入口见 `reference/MainPrompt.md`。
6. 根目录 CLAUDE.md 增加对 `.claude/react-app/` 的说明。
7. 视需要合并或保留两处 `settings.local.json`（见 2.5）。
8. 确认从仓库根打开项目时，命令/技能中路径均能正确解析；若有从 AppReact 子目录打开的习惯，再决定是否在 AppReact/.claude 留简短说明或链接。
9. （可选）在迁移验证通过后，删除或归档 `PlmApplication/AppReact/.claude/` 下已迁移内容，仅保留必要说明或本地配置。

---

**结论**：  
- 这些帮助文件的作用是：**命令**驱动迁移/分析/修 UI 的工作流，**reference** 提供迁移与 UI 的完整文档与场景模板，**skills** 提供迁移与上下文菜单的浓缩规则。  
- 可以迁移到 Solution 根目录 `.claude` 下，用 **`.claude/react-app/`** 单独存放并标明“给 React 用”，与今后其它 AI 帮助分开；迁移时需统一把文档内引用改为 `.claude/react-app/...`，并更新根 CLAUDE.md 与（可选）settings 合并。
