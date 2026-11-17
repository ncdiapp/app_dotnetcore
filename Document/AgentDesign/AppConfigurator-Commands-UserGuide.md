# Data Model Operation — User & Agent Guide

**Status:** Living knowledge base (authored from FE+BE code).  
**Date:** 2026-11-17  
**Parent plan:** [AppConfigurator-Commands-Knowledge-Plan.md](./AppConfigurator-Commands-Knowledge-Plan.md)  
**Parent agent design:** [AppConfigurator-AiAgent-Design.md](./AppConfigurator-AiAgent-Design.md) §7.1 E  

This document is the authoritative help for **Data Model Operation** and for the **app-configurator** agent when creating or editing operations.

**Code anchors**
- UI types / taxonomy: `AppReact/src/components/transaction/transactionCommandActionTypes.ts`, `transactionCommandTypeTaxonomy.ts`
- Editor: `ApplicationFormBuilder/CommandEditor.tsx` + `CommandEditorPart/*`
- List: `TransactionCommandMgt.tsx`
- Runtime: `APP.BL/TenantBusiness/AppTransactionCommandBL.cs`
- Menu: `FormMasterDetail/FormMainMenus.tsx`
- Attribute JSON: `AppActionAttributeDto` → column `FormulaExpression`

---

## 1. Concepts

| Term | Meaning |
|------|---------|
| **Data Model Operation (Operation)** | One row in `AppProjectWorkFlowAction` belonging to a transaction (data model). |
| **ActionType** | `EmAppTransactionCommandType` int. Selects executor + type-specific editor. |
| **Public (`LinkToUI`)** | Eligible for form menu / auto-on-open (with other flags). |
| **Private** | Internal / composition / message-link children; not on form menu by default. |
| **Category** | UI-only breadcrumb from taxonomy (not stored). Hidden for Composition. |
| **Composition** | ActionType 200: ordered child operations. |
| **Batch** | Run once per DataSet row, Search row, or child-grid row. |

### Taxonomy (picker)

| L1 | L2 (optional) | Types (Id) |
|----|---------------|------------|
| UI Operations | — | Close 54, Navigate To Search 53, Print 48, Print From Message Template 74, Save As 47 |
| Backend Operations | Form Persistence & Validation | Save 49, Refresh 50, Validate Required Fields 205 |
| Backend Operations | Form Data Processing | All Formula 44, Command Formula 57, All DataLoad 45, Generate Matrix 46, Execute SQL 42, Data Transfer 43 |
| Backend Operations | Import & File Generation | Json/Excel/Rest/DB imports 66/67/71/72, Multi Json/Excel 76/77, Download 73, Xml↔Json 81/82 |
| API, Plugin & External Process | — | Call API 59, Plugin Web Api 40, External Method 38, External Exe 68 |
| Messaging | — | Message UserId 60, Email 61, Partner 62, SMS 63 |
| Composition | — | Composition Command 200 (selecting L1 sets type) |

**Not offered in new picks:** 51, 55 (Open Form windows), 70 (Create User).

**Contexts:** List Edit picker = 43, 49, 50, 200 only. Workflow embedded omits UI window types 47/48/53/54/74.

### Agent `upsert_command` ActionType (critical)

`actionType` is **`EmAppTransactionCommandType`**, **not** Link Target codes (`1=Edit`, `2=Create`).

| Common need | actionType |
|-------------|------------|
| Refresh | **50** |
| Save | **49** |
| Execute SQL | **42** |
| Data Transfer | **43** |
| Call API | **59** |
| Composition | **200** |
| Close window | **54** |
| Send message (email field) | **61** |
| Validate required | **205** |

---

## 2. Shared settings (every operation)

Storage: **columns** on `AppProjectWorkFlowAction`, or **ActionAttribute JSON** in `FormulaExpression`.

### 2.1 Operation Basic Options

| UI | Storage | Default (new) | Runtime |
|----|---------|---------------|---------|
| Operation Name | column `Name` (max 500) | `NewOperation{n}` | Form menu label; message-link text |
| Operation Type | column `ActionType` | 42 | Selects executor + editor sections |
| Category | not stored | from taxonomy | Display only; hidden for Composition |
| Flow Order | column `ActionFlowOrder` | max+1 | List / form menu order (not composition child `Sort`) |
| Execute Condition Field | column `CommandConditionTransactionFieldId` | null = always run | Root/sibling **CheckBox** only. Must be boolean true or operation fails / menu disables |
| Is Public Operation (Link To UI) | JSON `LinkToUI` | Public tab true / Private false | Required for form menu + auto-on-open |
| Visible on Form Menu | JSON `IsShowOnTopMenu` | true | Needs LinkToUI; menu sorts by Flow Order |
| Visible on Calendar/Gantt/Scheduler Menu | JSON `IsShowOnSearchViewEventOptionMenu` | false | Server search views; does not require LinkToUI in BL |
| Auto Execute On Form Open | JSON `IsAutoExecuteOnFormOpen` | false | first matching public+flag operation on form load |

**Pitfalls:** Changing ActionType does not clear type-specific attributes. Unchecking Public does not clear the three child flags. List organized type skips condition check.

### 2.2 Operation Status Log

| UI | Storage | Runtime |
|----|---------|---------|
| Log Operation Status (dropdown) | **Not persisted** (`EmAppCommandLoggingPreference` commented out on DTO) | Only drives the two checkboxes in the editor |
| Log Operation Start & End | JSON `IsLogCommandStartEnd` | Workflow automation root only → AppLogTrack |
| Log Info & Error Details | JSON `IsLogErrorDetails` | Workflow root error details |

**Pitfall:** After reload, preference dropdown often shows “Do Not Log” even when flags are set.

### 2.3 Execution Result Display

| UI | Storage | Runtime |
|----|---------|---------|
| Execute Result Console Display Type | JSON `EmAppValidationResultPreference` (1 details, 2 status only) | On error + status-only → “Failed to Execute Operation.” |
| Enable customized success message | JSON `IsEnableCommandExecuteResultMessage` | Editor only; runtime uses non-empty `CommandSuccessMessage` |
| Success message text | JSON `CommandSuccessMessage` | Placeholder replace on success |

**Pitfall:** `CommandFailedMessage` exists in runtime/DTO but has **no** React editor field.

### 2.4 Batch Operation Setting

| UI | Storage | Runtime |
|----|---------|---------|
| Is Batch Operation | JSON `IsBatchCommand` | DataSet/Search → loop PKs and re-run; ChildUnit → loop child rows |
| Source Type | JSON `BatchCommandSourceFromType` (1 DataSet, 2 Search, 3 ChildUnit) | |
| DataSet / field name | `BatchCommandDataSetId`, `BatchCommandSourceDataSetFieldName` | PK column from dataset row |
| Search / view field / criteria map | `BatchCommandSearchId`, `BatchCommandSourceViewFieldId`, `DictBatchSearchCrietraIdAndTransFieldId` | |
| Source Unit | `ForeachLoopSourceUnitId` | Must be a **child** unit present in `DictOneToManyFields` |

### 2.5 Notes

| UI | Storage | Runtime |
|----|---------|---------|
| Notes textarea | column `Description` (max 500) | Documentation only |

### 2.6 Public / Private tabs

`TransactionCommandMgt` filters `CommandActionList` by `LinkToUI`. New operations inherit the active tab.

---

## 3. Master → child

### 3.1 Composition (ActionType 200)

Editor: `CompositionCommandSection.tsx`.

- `ChildActionList[]`: `Sort`, `CommandId`, `CommandDisplay`, `ExternalTransactionId`, `IsSkip`, `IsGoToNextCommandWithError`, `PredictValue`, `ChangeTriggerRootLevelFieldId`, `ChangeTriggerChildGridUnitId`, …
- `ChildCommandsSwitchConditionFieldId` (root DDL) for switch/`PredictValue`
- Children usually **private** (`LinkToUI: false`)
- Runtime: `ExecuteCompositionCommand` — order by `Sort`; skip / switch / field-change / grid-change gates; stop on error unless Go To Next With Error
- FE may open forms from `ChildCommandResultDtoList` when `IsNeedToOpenTransactionForm`

### 3.2 Message link children (types 61, 62)

Editor: `MessageLinkCommandsSection.tsx`. Same `ChildActionList` shape, but at **send** time mostly becomes encrypted operation links in the message. Skip/triggers are largely unused until the link is executed later.

---

## 4. Execution pipeline

1. **Form menu** (`FormMainMenus`): public + show on menu → condition →  
   - **FE-only:** Save 49, Refresh 50, Print 48, Save As 47, Close 54, Navigate To Search 53  
   - **Else:** POST execute → server  
2. **Server** (`ExecuteTransactionRootCommand`): condition → batch loop (if DataSet/Search) → Composition **or** `ExecuteSingleCommandByActionType`  
3. **Auto on open:** first `LinkToUI && IsAutoExecuteOnFormOpen` during form load  
4. **Workflow root:** always composition path + optional start/end logging  

List Edit server switch only implements **43, 49, 50** well; other types may no-op on list-edit.

---

## 5. Per-type reference

### 5.1 UI Operations

#### 54 — UI: Close Form Window
- **Config:** common only  
- **Where:** FE-only (close window / tab)  
- **Note:** As composition child, does not close UI on server  

#### 53 — UI: Navigate To Search
- **Config:** `OpenLinkedSearchSection` → `LinkedSearchId`  
- **Where:** FE-only  
- **Need:** Linked search on root unit  

#### 48 — UI: Print
- **Config:** common only  
- **Where:** FE-only; requires saved root PK  

#### 74 — UI: Print From Message Template
- **Config:** `PrintFromMessageTemplateSection` → `MessageTemplateId`  
- **Where:** Menu goes to **server**; master-detail switch has **no** PrintFromMessageTemplate branch (returns without rendering template)  

#### 47 — UI: Save As
- **Config:** common only  
- **Where:** FE menu save-as; server path for composition/auto-exec opens new form via result flags  

### 5.2 Backend — Form Persistence & Validation

#### 49 — Save
- **Menu:** FE `handleSave`  
- **Server:** composition / batch / auto-exec → `SaveTransactionData`  

#### 50 — Refresh
- **Menu:** FE `handleRefresh`  
- **Server:** reload form data  

#### 205 — Validate Required Fields
- **Server:** `MasterDetailDataFirstLevelValidation`  
- **Not** in list-edit picker effectively  

### 5.3 Backend — Form Data Processing

#### 44 — Exec DataModel All Formula
- Runs all data-model formulas; marks dirty; does not save by itself  

#### 57 — Execute Command Formula
- **Config:** `ExecutionFormula` / `ExecutionFormulaUI` assignment expression  
- **Need:** non-empty formula  

#### 45 — Execute All DataLoad
- Runs template data loads  

#### 46 — Generate Matrix
- **Config:** `ParamId1` = matrix unit id  

#### 42 — Execute SQL Statement
- **Config:** SQL in **`NotificationMessage`**; optional `AssignSqlResultToFiledId`  
- Tokens `[TF_…]` replaced from form  

#### 43 — Execute Data Transfer
- **Config:** `DataTransferSettingId`; optional `TargetTransactionCommandId`, `IsNeedToOpenNewForm`  
- Allowed on list-edit  

### 5.4 Backend — Import & File Generation

Shared UI: `OperationFileAndImportSection` + FTP path helpers. Paths token-substituted before execute.

| Id | Display | Key fields |
|----|---------|------------|
| 66 | Import from Json | `JsonImportSettingId`, `FilePath` |
| 67 | Import from Excel | `ExcelImportSettingId`, `FilePath` |
| 71 | Import from Rest-Api setting | `RestApiDbImportSettingId` |
| 72 | Import from DB-To-DB setting | `DbToDbImportSettingId` |
| 76 | Multi Json files in folder | `FilePath` (folder; file names match settings) |
| 77 | Multi Excel files in folder | `FilePath` |
| 73 | Download File To Server Folder | `FilePath`, `DistinationFilePath` (+ FTP creds) |
| 81 | Convert Xml → Json | `FilePath`, `DistinationFilePath` |
| 82 | Convert Json → Xml | `FilePath`, `DistinationFilePath` |

### 5.5 API, Plugin & External Process

#### 59 — Call API Operation
- **Config:** `DataTransferSettingId` (API mapping); embedded transfer editor  
- **Runtime:** `CallApiOperationFromMasterDetailTransactionForm`  

#### 40 — Call Plugin Web Api
- **Config:** `WebApiMethodName`  
- **Runtime:** plugin client external service  

#### 38 — External Method MasterDetail
- **Config:** `ExternalMethodRegisterId`  

#### 68 — Execute External Exe Process
- **Config:** `FilePath` (exe), `Arguments` (JSON)  
- **Runtime:** `ProcessHelper.CreateProcess`  

### 5.6 Messaging

Shared: subject/body/template/attach flags via `NotificationExpressionSection` + destination fields.

| Id | Destination field property |
|----|----------------------------|
| 60 | UserId field (`NotificationDestinationPartnerIdTransactionFiledId` — labeled UserId) |
| 61 | Email field + **Message Link Operations** children |
| 62 | Partner field + optional ToEmail + **Message Link Commands** |
| 63 | Phone field (`SmsMessageToPhoneNumberFiledId`); SMS body = `NotificationMessage` |

### 5.7 Composition

#### 200 — Composition Command
See §3.1. Selecting L1 **Composition** sets ActionType 200.

---

## 6. Configurator agent — how to use this KB

1. Before `upsert_command`, read this file via `list_agent_files` / `read_agent_file` (when present under AgentStarter `app-configurator`).  
2. Use correct **actionType** ints from §1.  
3. Put SQL in `notificationMessage` for type 42.  
4. Use `optionsJson` for `linkToUI`, `isShowOnTopMenu`, `isAutoExecuteOnFormOpen`, `actionFlowOrder`, and ActionAttribute patches.  
5. Preview → `ask_user` → `confirmed=true` → `request_form_reload`.  
6. Never execute runtime operations from Configurator.

---

## 7. Change log

| Date | Note |
|------|------|
| 2026-11-17 | Initial KB from FE+BE pass (shared settings + 32 taxonomy types). |
| 2026-11-17 | Synced to `AppReact/public/help/commands-user-guide.md` and `AppAI.Web/AgentStarterSeeds/app-configurator/commands-knowledge.md`. |

| 2026-11-17 | Product rename: Command / Operation Task → Data Model Operation (short: Operation). Tool names unchanged.
