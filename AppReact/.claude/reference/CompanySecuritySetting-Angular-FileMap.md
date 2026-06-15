# Company Security Setting — Angular 文件位置清单

根据 **angular-to-react-component** 技能与 `AngularJsReferenceGuide.md` 规则，在 Angular 项目  
**Solution Root**: `C:\DevApp\App\`  
下定位 **Company Security Setting** 菜单及其所有子页面对应的 `.cshtml` 与 `.js` 文件。

---

## 一、菜单入口与路由

| 用途 | 文件路径 |
|------|----------|
| 左侧菜单项（Company Security Setting） | `{SolutionRoot}\PlmApplication\Server\Views\Home\Common\navigation.cshtml` |
| 路由定义（main.CompanyManagement 等） | `{SolutionRoot}\PlmApplication\Scripts1x\mgtRoute.js` |
| 菜单配置（builtInMenu.CompanySetting） | `{SolutionRoot}\PlmApplication\Scripts1x\Helper\mgtBaseNavigationHelper.js` |
| 菜单配置（Saas 等） | `{SolutionRoot}\PlmApplication\Scripts1x\Helper\saasNavigationHelper.js` |

---

## 二、主页面：Company Security Setting

对应图中「System Settings / Company Security Setting」整页，含顶部 Tab（Company Info、Users、Security Roles…）。

| 类型 | 文件路径 |
|------|----------|
| **.cshtml** | `{SolutionRoot}\PlmApplication\Server\Views\Administration\CompanyManagement.cshtml` |
| **.js** | `{SolutionRoot}\PlmApplication\Scripts1x\mgtCtrl\Administration\companyManagementCtrl.js` |
| **MVC Action** | `PlmApplication\Server\Controllers\AdministrationController.cs` → `CompanyManagement()` |

主页面通过 `companyManagementCtrl` 按当前 Tab 动态加载下方子页面（createRouteStateSvc 注入 iframe/state）。

---

## 三、主 Tab 对应子页面（第一层嵌入）

以下 9 个 Tab 各自对应一个子 state 和一组 .cshtml/.js。

### Tab 0 — Company Info

| 类型 | 文件路径 |
|------|----------|
| **.cshtml** | `{SolutionRoot}\PlmApplication\Server\Views\Administration\CompanyEditor.cshtml` |
| **.js** | `{SolutionRoot}\PlmApplication\Scripts1x\mgtCtrl\Administration\companyEditorCtrl.js` |
| **Route** | `main.CompanyEditor` |

---

### Tab 1 — Users（内嵌第二层：User 子 Tab）

Users Tab 内还有子 Tab：Employee / Client / Supplier / Client Agent / Supplier Agent。  
由 `companyManagementCtrl` 的 `loadUserSubTabContainer()` 按子 Tab 加载：

| 子 Tab | Route | .cshtml | .js |
|--------|-------|---------|-----|
| Employee | `main.CompanyOrgnizationSetup` | `Administration\CompanyOrgnizationSetup.cshtml` | `mgtCtrl\Administration\companyOrgnizationSetupCtrl.js` |
| Customer/Supplier/ClientAgent/SupplierAgent | `main.BusinessPartnerManagement` | `Administration\BusinessPartnerManagement.cshtml` | `mgtCtrl\Administration\businessPartnerManagementCtrl.js` |

**完整路径：**

| 类型 | 文件路径 |
|------|----------|
| **.cshtml** (Employee) | `{SolutionRoot}\PlmApplication\Server\Views\Administration\CompanyOrgnizationSetup.cshtml` |
| **.js** (Employee) | `{SolutionRoot}\PlmApplication\Scripts1x\mgtCtrl\Administration\companyOrgnizationSetupCtrl.js` |
| **.cshtml** (Partner) | `{SolutionRoot}\PlmApplication\Server\Views\Administration\BusinessPartnerManagement.cshtml` |
| **.js** (Partner) | `{SolutionRoot}\PlmApplication\Scripts1x\mgtCtrl\Administration\businessPartnerManagementCtrl.js` |

---

### Tab 2 — Security Roles

| 类型 | 文件路径 |
|------|----------|
| **.cshtml** | `{SolutionRoot}\PlmApplication\Server\Views\Administration\CompanyRoleSetup.cshtml` |
| **.js** | `{SolutionRoot}\PlmApplication\Scripts1x\mgtCtrl\Administration\companyRoleSetupCtrl.js` |
| **Route** | `main.CompanyRoleSetup` |

---

### Tab 3 — User Defined Menu Roles

| 类型 | 文件路径 |
|------|----------|
| **.cshtml** | `{SolutionRoot}\PlmApplication\Server\Views\Administration\CompanyMenuRoleSetup.cshtml` |
| **.js** | `{SolutionRoot}\PlmApplication\Scripts1x\mgtCtrl\Administration\companyMenuRoleSetupCtrl.js` |
| **Route** | `main.CompanyMenuRoleSetup` |

---

### Tab 4 — Contact Groups

| 类型 | 文件路径 |
|------|----------|
| **.cshtml** | `{SolutionRoot}\PlmApplication\Server\Views\Administration\CompanyContactGroupSetup.cshtml` |
| **.js** | `{SolutionRoot}\PlmApplication\Scripts1x\mgtCtrl\Administration\companyContactGroupSetupCtrl.js` |
| **Route** | `main.CompanyContactGroupSetup` |

---

### Tab 5 — Application Menu

| 类型 | 文件路径 |
|------|----------|
| **.cshtml** | `{SolutionRoot}\PlmApplication\Server\Views\Administration\DomainAndUserMenuManagement.cshtml` |
| **.js** | `{SolutionRoot}\PlmApplication\Scripts1x\mgtCtrl\Administration\domainAndUserMenuManagementCtrl.js` |
| **Route** | `main.DomainAndUserMenuManagement` |

---

### Tab 6 — Dashboard（内嵌第二层：按用户类型子 Tab）

Dashboard Tab 内按用户类型子 Tab 加载 **DashboardEditor**（注意：视图在 **Desktop** 下，非 Administration）：

| 类型 | 文件路径 |
|------|----------|
| **.cshtml**（主视图） | `{SolutionRoot}\PlmApplication\Server\Views\Desktop\DashboardEditor.cshtml` |
| **.cshtml**（局部） | `{SolutionRoot}\PlmApplication\Server\Views\Desktop\DashboardEditor\_Toolbar_Canvas.cshtml` |
| | `{SolutionRoot}\PlmApplication\Server\Views\Desktop\DashboardEditor\_Toolbar_Flex.cshtml` |
| | `{SolutionRoot}\PlmApplication\Server\Views\Desktop\DashboardEditor\_Toolbar_Flex_ClusterAnalysisView.cshtml` |
| | `{SolutionRoot}\PlmApplication\Server\Views\Desktop\DashboardEditor\_DesignPanel_Canvas.cshtml` |
| | `{SolutionRoot}\PlmApplication\Server\Views\Desktop\DashboardEditor\_DesignPanel_Flex.cshtml` |
| | `{SolutionRoot}\PlmApplication\Server\Views\Desktop\DashboardEditor\_DesktopItemList.cshtml` |
| **.js** | `{SolutionRoot}\PlmApplication\Scripts1x\mgtCtrl\Desktop\dashboardEditorCtrl.js` |
| **Route** | `main.DashboardEditor`（templateUrl: `/Desktop/DashboardEditor`） |

---

### Tab 7 — Privileges

| 类型 | 文件路径 |
|------|----------|
| **.cshtml** | `{SolutionRoot}\PlmApplication\Server\Views\Administration\PrivilegeManagement.cshtml` |
| **.js** | `{SolutionRoot}\PlmApplication\Scripts1x\mgtCtrl\Administration\privilegeManagementCtrl.js` |
| **Route** | `main.PrivilegeManagement` |

（PrivilegeManagement 可能内嵌或弹窗用到 PrivilegeEditor、SystemObjectSecurityEditor 等，按需再查。）

---

### Tab 8 — Integration Tokens

| 类型 | 文件路径 |
|------|----------|
| **.cshtml** | `{SolutionRoot}\PlmApplication\Server\Views\Administration\IntegrationTokenManagement.cshtml` |
| **.js** | `{SolutionRoot}\PlmApplication\Scripts1x\mgtCtrl\Administration\integrationTokenManagementCtrl.js` |
| **Route** | `main.IntegrationTokenManagement` |

---

## 四、可能被弹窗/内嵌引用的相关页面（第三层及关联）

子页面中可能再打开编辑器或其它管理页，常见有：

| 功能 | .cshtml | .js |
|------|---------|-----|
| User 编辑 | `Administration\UserEditor.cshtml` | `mgtCtrl\Administration\userEditorCtrl.js` |
| User 登录信息 | `Administration\UserLoginInfoEditor.cshtml` | `mgtCtrl\Administration\userLoginInfoEditorCtrl.js` |
| 业务伙伴编辑 | `Administration\BusinessPartnerEditor.cshtml` | `mgtCtrl\Administration\businessPartnerEditorCtrl.js` |
| 权限编辑 | `Administration\PrivilegeEditor.cshtml` | `mgtCtrl\Administration\privilegeEditorCtrl.js` |
| 系统对象安全编辑 | `Administration\SystemObjectSecurityEditor.cshtml` | `mgtCtrl\Administration\systemObjectSecurityEditorCtrl.js` |
| Domain Dashboard 管理（若从别处链入） | `Administration\DomainDashboardManagement.cshtml` | `mgtCtrl\Administration\domainDashboardManagementCtrl.js` |

---

## 五、服务与后端

| 用途 | 文件路径 |
|------|----------|
| 管理/公司/用户等 API | `{SolutionRoot}\PlmApplication\Scripts1x\Services\adminSvc.js` |
| MVC Controller（所有 Administration 的 Action） | `{SolutionRoot}\PlmApplication\Server\Controllers\AdministrationController.cs` |
| Desktop Dashboard 的 Action | `{SolutionRoot}\PlmApplication\Server\Controllers\DesktopController.cs`（如 `DashboardEditor`） |

---

## 六、汇总：仅列出 .cshtml 与 .js（便于迁移时查找）

**全部 .cshtml（按目录）：**

- `PlmApplication\Server\Views\Home\Common\navigation.cshtml`
- `PlmApplication\Server\Views\Administration\CompanyManagement.cshtml`
- `PlmApplication\Server\Views\Administration\CompanyEditor.cshtml`
- `PlmApplication\Server\Views\Administration\CompanyOrgnizationSetup.cshtml`
- `PlmApplication\Server\Views\Administration\CompanyRoleSetup.cshtml`
- `PlmApplication\Server\Views\Administration\CompanyMenuRoleSetup.cshtml`
- `PlmApplication\Server\Views\Administration\CompanyContactGroupSetup.cshtml`
- `PlmApplication\Server\Views\Administration\BusinessPartnerManagement.cshtml`
- `PlmApplication\Server\Views\Administration\BusinessPartnerEditor.cshtml`
- `PlmApplication\Server\Views\Administration\DomainAndUserMenuManagement.cshtml`
- `PlmApplication\Server\Views\Administration\PrivilegeManagement.cshtml`
- `PlmApplication\Server\Views\Administration\PrivilegeEditor.cshtml`
- `PlmApplication\Server\Views\Administration\IntegrationTokenManagement.cshtml`
- `PlmApplication\Server\Views\Administration\UserEditor.cshtml`
- `PlmApplication\Server\Views\Administration\UserLoginInfoEditor.cshtml`
- `PlmApplication\Server\Views\Administration\SystemObjectSecurityEditor.cshtml`
- `PlmApplication\Server\Views\Administration\DomainDashboardManagement.cshtml`
- `PlmApplication\Server\Views\Desktop\DashboardEditor.cshtml`
- `PlmApplication\Server\Views\Desktop\DashboardEditor\_Toolbar_Canvas.cshtml`
- `PlmApplication\Server\Views\Desktop\DashboardEditor\_Toolbar_Flex.cshtml`
- `PlmApplication\Server\Views\Desktop\DashboardEditor\_Toolbar_Flex_ClusterAnalysisView.cshtml`
- `PlmApplication\Server\Views\Desktop\DashboardEditor\_DesignPanel_Canvas.cshtml`
- `PlmApplication\Server\Views\Desktop\DashboardEditor\_DesignPanel_Flex.cshtml`
- `PlmApplication\Server\Views\Desktop\DashboardEditor\_DesktopItemList.cshtml`

**全部 .js（按目录）：**

- `PlmApplication\Scripts1x\mgtRoute.js`
- `PlmApplication\Scripts1x\Helper\mgtBaseNavigationHelper.js`
- `PlmApplication\Scripts1x\Helper\saasNavigationHelper.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\companyManagementCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\companyEditorCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\companyOrgnizationSetupCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\companyRoleSetupCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\companyMenuRoleSetupCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\companyContactGroupSetupCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\businessPartnerManagementCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\businessPartnerEditorCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\domainAndUserMenuManagementCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\privilegeManagementCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\privilegeEditorCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\integrationTokenManagementCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\userEditorCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\userLoginInfoEditorCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\systemObjectSecurityEditorCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Administration\domainDashboardManagementCtrl.js`
- `PlmApplication\Scripts1x\mgtCtrl\Desktop\dashboardEditorCtrl.js`
- `PlmApplication\Scripts1x\Services\adminSvc.js`

**说明：**  
- `{SolutionRoot}` = `C:\DevApp\App\`  
- `.generated.cs` 为 .cshtml 的自动生成产物，迁移时以 .cshtml 为准。  
- 实际嵌入层级：主页面 → Tab 子页（第一层）→ Users/Dashboard 子 Tab 或弹窗（第二/三层），已按此结构列出对应 .cshtml 与 .js。

---

*文档依据：CompanyManagement.cshtml、companyManagementCtrl.js、mgtRoute.js、AdministrationController 与 AngularJsReferenceGuide 路径规则生成。*
