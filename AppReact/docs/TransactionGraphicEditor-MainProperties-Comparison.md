# Main Properties 区：Angular 与 React 正确对比

Angular 来源：`C:\DevApp\App\PlmApplication\Server\Views\Transaction\TransactionEditor\_Saas_MainProperties.cshtml`

---

## 一、Angular 的两种完全不同的布局（按 IsPhysicalModelTableCreated 分支）

Angular 用**两个互斥的根节点**，不是同一套表单里用 ng-if 藏字段：

| 条件 | 对应场景 | 根节点 |
|------|----------|--------|
| `ng-if="dataModel.AppTransactionData.IsPhysicalModelTableCreated"` | From Database、物理表已创建 | **第一块** (line 7-230) |
| `ng-if="!dataModel.AppTransactionData.IsPhysicalModelTableCreated"` | Temp DTO、**Create Data Model Hierarchy From Rest API** | **第二块** (line 233-339) |

---

## 二、第一块（IsPhysicalModelTableCreated = true）包含内容

| 字段/区块 | 显示条件 | 说明 |
|-----------|----------|------|
| Data Model Name | 始终 | 文本 |
| Data Model Type | 始终 | 下拉，disabled |
| File Storage Folder | 始终 | 只读+选文件夹+清除 |
| Grandchild Edit Mode | 始终 | 下拉 |
| PreSaveValidationMethod | 始终 | 文本 |
| Is For Public Acesss | `TransactionOrganizedType == MasterDetail` | 复选框 |
| Enable CPF Flow | MasterDetail | 复选框 |
| Is Exclusive For Owner | MasterDetail | 复选框 |
| Enable Comunication | MasterDetail | 复选框 |
| Is Show Save Button | MasterDetail | 复选框 |
| Is Show Calculate Button | MasterDetail | 复选框 |
| Is Show Print Button | MasterDetail | 复选框 |
| **Enable Publish Data To API** | **MasterDetail 且 !IsApiIntegrationTransaction** | 复选框 + 配置链接/下拉 |
| Communication Dock Position | IsNeedToSetComunication | 下拉 |
| Communication Group By User | IsNeedToSetComunication | 下拉 |
| Communication User List Field | IsNeedToSetComunication | 下拉 |
| Open Communication By Default | 始终（在 Communication 区块内） | 复选框 |

---

## 三、第二块（!IsPhysicalModelTableCreated，含 Rest API）包含内容

第二块**没有**：File Storage、Grandchild Edit Mode、PreSaveValidationMethod、CPF/Exclusive/Communication、Show Save/Calculate/Print、Enable Publish Data To API、Communication 详情。

第二块**有**：

| 字段/区块 | 显示条件 | 说明 |
|-----------|----------|------|
| Data Model Name | 始终 | 文本 |
| Data Model Type | 始终 | 下拉，disabled |
| **Provider API Data Source** | **IsApiIntegrationTransaction** | FolderUsageType 选择器（编辑/下拉选 API Operation） |
| **API JSON Data** | IsApiIntegrationTransaction 且 BaseApiConfigDto.JsonSampleData | “View JSON Data” 链接 |
| **API CRUD Type** | IsApiIntegrationTransaction 且 BaseApiConfigDto.JsonSampleData | 下拉 EmAppTransactionCrudType |
| **Need Save With Provider API** | IsApiIntegrationTransaction 且 ApiIntegrationTransactionCrudType == 2 | 复选框 + 配置链接/下拉 |
| **Need Delete With Provider API** | IsApiIntegrationTransaction 且 ApiIntegrationTransactionCrudType == 2 | 复选框 + 配置链接/下拉 |

---

## 四、React 当前 Main Properties 的问题（正确对比）

| 对比项 | Angular | React 当前 | 结论 |
|--------|---------|------------|------|
| 是否按 IsPhysicalModelTableCreated 分两套 UI | 是，两套互斥根节点 | 否，一套布局 + 部分 isMasterDetail / !isApiIntegrationTransaction | **未按“物理表/非物理表”分两套** |
| 何时显示 Main Properties 整体 | 第一块：IsPhysicalModelTableCreated；第二块：!IsPhysicalModelTableCreated | shouldShowMainProperties = isCreateNewItem \|\| isPhysicalModelTableCreated | **非物理表时仍可能显示同一套**（且 isCreateNewItem 会显示） |
| Rest API 时是否隐藏 File Storage | 是（走第二块，无此字段） | 否，始终显示 | **应隐藏** |
| Rest API 时是否隐藏 Grandchild Edit Mode / PreSave | 是（第二块无） | 否，始终显示 | **应隐藏** |
| Rest API 时是否隐藏 CPF/Exclusive/Public/Communication/Show buttons | 是（第二块无） | 仅按 isMasterDetail 显示，未按“是否物理表”区分 | **API 时应整块不显示（第二块无这些）** |
| Rest API 时是否隐藏 “Enable Publish Data To API” | 第一块才存在且 !IsApiIntegrationTransaction；第二块无 | 用 !isApiIntegrationTransaction 隐藏 | **逻辑近似，但 React 未分块，API 时仍显示其它不应有的字段** |
| Rest API 时是否显示 Provider API Data Source | 是（第二块，IsApiIntegrationTransaction） | 否 | **缺失** |
| Rest API 时是否显示 API JSON Data / API CRUD Type | 是 | 否 | **缺失** |
| Rest API 时是否显示 Need Save/Delete With Provider API | 是（且 CRUD Type == 2） | 否 | **缺失** |

---

## 五、修改计划（Main Properties 区）

1. **按 IsPhysicalModelTableCreated 拆成两套 UI（与 Angular 一致）**
   - **当 `isPhysicalModelTableCreated === true`**：渲染当前这套（Data Model Name, Type, File Storage, Grandchild, PreSave, MasterDetail 的 CPF/Exclusive/Public/Show buttons, Enable Publish Data To API when !isApiIntegrationTransaction, Communication 区块）。  
   - **当 `isPhysicalModelTableCreated === false`**（含 Rest API / Temp DTO）：**不渲染**上述内容，改为渲染“第二块”：仅 Data Model Name、Data Model Type；若 `isApiIntegrationTransaction` 再增加：Provider API Data Source、API JSON Data（有 JsonSampleData 时）、API CRUD Type、当 ApiIntegrationTransactionCrudType === 2 时显示 Need Save With Provider API、Need Delete With Provider API。

2. **Main Properties 整体显示条件**
   - 与 Angular 一致：两种情况下都显示 Main Properties，只是内容不同。  
   - 可保留 `shouldShowMainProperties = isCreateNewItem || isPhysicalModelTableCreated`，但内部必须按 `isPhysicalModelTableCreated` 分支渲染上述两套之一。

3. **Rest API 专用字段与数据**
   - 从 transactionData / formData 或 API 取：FolderUsageType（Provider API Data Source）、BaseApiConfigDto?.JsonSampleData、OtherOptions?.ApiIntegrationTransactionCrudType、IsEnableSaveByApiCall、SaveByApiCallDataTransferId、IsEnableDeleteByApi、DeleteDataTransferId 等。
   - 实现 Provider API Data Source 的编辑/下拉、View JSON Data、API CRUD Type 下拉、Need Save/Delete 的配置链接或下拉（与 Angular 行为一致）。

4. **List 类型（TransactionOrganizedType === 3）**
   - Angular 第一块里 CPF/Exclusive/Communication/Show buttons 均为 MasterDetail；第二块本身就没有这些。React 已用 isMasterDetail 包住 Section 2/3，无需改逻辑，只需确保第二块（非物理表）不包含这些区块。

---

## 六、小结：未正确比较的原因

之前比较时没有发现 Angular 使用的是**两套完全不同的根布局**（`IsPhysicalModelTableCreated` vs `!IsPhysicalModelTableCreated`），而不是在同一套表单里用 ng-if 隐藏部分字段。Rest API 时 Angular 只显示第二块（Name + Type + API 专用字段），React 目前仍显示第一块的全部内容且缺少第二块的 API 专用字段，因此需要按上述计划拆分并补齐。
