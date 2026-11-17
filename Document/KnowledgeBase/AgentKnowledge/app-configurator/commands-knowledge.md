# Data Model Operation — Agent Knowledge Base

**Audience:** `app-configurator` agent and developers. **Not** in-app user Help.  
**Canonical path:** `Document/KnowledgeBase/AgentKnowledge/app-configurator/commands-knowledge.md`  
**User Help:** `Document/KnowledgeBase/UserGuide/DataModelOperation-Help.md` → `AppReact/public/help/commands-user-help.md`  
**Design plan:** `Document/AgentDesign/AppConfigurator-Commands-Knowledge-Plan.md`  
**Read before:** `upsert_command`, `get_command_detail`.  
**Sync:** `Document/KnowledgeBase/SyncRuntimeDocs.ps1`

`actionType` is **`EmAppTransactionCommandType`** (int). **Not** link-target codes (1=Edit, 2=Create).

### Field tokens (Query Builder / Message / Formula) — hard rules

The Operation editor **Query Builder with Built-in Tokens** popup shows **unit-layered field buttons** (Form Root Level Fields, Grid `{unit}` Columns, builtins). Clicking a button inserts the live token at the cursor. Agents must emit the **same** strings — resolve via tools, never invent.

| Surface | Tool fields | Format |
|---------|-------------|--------|
| **Execute SQL (42)** | `sqlToken` | `[TF_{fieldId}_{dbColumn}]` e.g. `[TF_3499_OpportunityId]` |
| **Message / notification** | `messageToken` (same as `sqlToken` for fields) | `[TF_…]`; builtins include `[CurrentPkValue]`, `[Today]`, … |
| **Command Formula (57)** | `formulaToken` | Root: `[DbColumn_fieldId]`; grid/sibling: `[Table.DbColumn_fieldId]` |

**Tools:** `get_current_transaction_config` (includes **`rootPrimaryKeys[]`**), `find_fields`, `get_unit_fields` — each field has `sqlToken`, `messageToken`, `formulaToken`, `isPrimaryKey`, `unitKind` (`rootMaster` \| `masterSibling` \| `childGrid`).

**Current form row / root master id:** use `rootPrimaryKeys[].sqlToken` (fields with `isPrimaryKey` on `unitKind=rootMaster`). That is the Query Builder **OpportunityId** (etc.) button — **not** `[rootid]`.

**SQL hard rules (42):** SQL goes in `notificationMessage`. `UPDATE`/`DELETE` **must** `WHERE` the intended row(s); for “this form record” use root PK `sqlToken`. Forbidden: `[rootid]`, missing `WHERE`, hand-waved placeholders.

### Taxonomy quick map

| L1 | L2 | actionType ids |
|----|-----|----------------|
| UI Operations | — | 54, 53, 48, 74, 47 |
| Backend | Form Refresh, Validate & Save | 49, 50, 205 |
| Backend | Form Data Processing | 44, 57, 45, 46, 42, 43 |
| Backend | Import & File Generation | 66, 67, 71, 72, 76, 77, 73, 81, 82 |
| API, Plugin & External | — | 59, 40, 38, 68 |
| Messaging | — | 60, 61, 62, 63 |
| Composition | — | 200 |

Excluded from new picker: **51**, **55**, **70**.

---

## 1. Introduction

Same product terms as User Help. Storage table: **`AppProjectWorkFlowAction`**.  
Attribute JSON: **`FormulaExpression`** column deserialized to **`AppActionAttributeDto`**.  
Column **`NotificationMessage`**: SQL text (type 42), SMS body (63), general notification body for other types.

**Agent workflow:** `get_commands` → `get_command_detail` → read this KB → `upsert_command` with preview → `ask_user` → `confirmed=true` → `request_form_reload`. Never execute runtime operations from Configurator.

---

## 2. Shared settings (every operation)

(collapsible sections appear in the right-hand Operation editor when you select an operation in the left list)

### 2.1 Operation Basic Options

Expand **Operation Basic Options** at the top of the editor.

| Control | Storage / runtime |
|---------|----------------|
| **Operation Name** | Label on the form Operation menu and in the operation list. Use short, user-facing names (for example, *Submit Opportunity*). |
| **Operation Type** | Searchable tree picker. Shows the current type display name. Click to open the tree (UI Operations, Backend Operations with sub-groups, API/Plugin, Messaging, Composition). Type to filter. Arrow keys and Enter select a leaf; Esc closes. Selecting **Composition** sets type to Composition Operation immediately. Changing type shows or hides sections below; **old type-specific values are not cleared automatically**. |
| **Category** | Read-only breadcrumb derived from Operation Type (for example, `Backend Operations / Form Data Processing`). Not saved. Hidden for Composition. |
| **Flow Order** | Numeric sort order in the operation list and on the form menu (lower numbers appear first). Hidden in Workflow Automation embedded editor. |
| **Execute Condition Field** | Optional root **checkbox** field. `No Condition (The task will execute)` = always run when invoked. If the checkbox is not true at run time, the menu item is disabled and the step is skipped (server composition/batch). |
| **Is Public Operation (Link To UI)** | When on, the operation can appear on menus or auto-run. When off, treat as an internal (private) step. New operations inherit the **Public Operation** / **Private Operation** tab active when you click **Add Operation**. |
| **Visible on Form Menu** | Shown only when Public. Adds the operation to the form Operation menu. |
| **Visible on Calendar/Scheduler/Gantt View Menu** | Shown only when Public. Adds the operation to calendar/Gantt/scheduler view event menus. |
| **Auto Execute On Form Open** | Shown only when Public. Runs once when the form opens (first matching public operation with this flag). |

**Operation Type help:** Click the **?** next to **Operation Type** to open this manual scrolled to the current type.

### 2.2 Operation Status Log

| Control | Storage / runtime |
|---------|----------------|
| **Log Operation Status** | Preference dropdown: *Log Result Details*, *Log Result Status Only*, or *Do Not Log*. Drives the two checkboxes below (not stored as its own column after save in all builds—reopen may show *Do Not Log* while flags remain). |
| **Log Operation Start & End** | When enabled, workflow automation **root** operations write start/end tracking. Disabled when preference is *Do Not Log*. |
| **Log Operation Info & Error Details** | Extra error detail for workflow roots. Disabled when preference is *Do Not Log*. |

For ordinary form operations, leave logging off unless you debug workflow automation.

### 2.3 Operation Execution Result Display

| Control | Storage / runtime |
|---------|----------------|
| **Execute Result Console Display Type** | *Show Result Details* = longer console text on failure/success paths. *Show Result Status Only* = brief status (failures may show only *Failed to Execute Operation.*). |
| **Customized Execution Result Message → Enable** | When on, shows a text area for a custom **success** message after a successful run. |
| Success message text area | Shown when Enable is on. Plain text shown to the user on success (token replacement at runtime when supported). |

### 2.4 Batch Operation Setting

| Control | Storage / runtime |
|---------|----------------|
| **Is Batch Operation** | When on, the operation runs **once per row** from a source instead of once for the single open form. |
| **Batch Operation Source Type** | **DataSet**, **Search**, or **Child Unit** (child grid). |
| **Batch Operation DataSet** | (DataSet) Pick a dataset; chevron opens dataset list popup; pencil opens dataset editor when a row is selected. |
| **Batch Operation TransactionRId DataSet Field** | (DataSet) Column in the dataset that holds the record id for each iteration. |
| **Batch Operation Search** | (Search) Pick a search view; chevron opens search list; optional **Batch Search Criteria Mapping** popup maps search criteria to form fields. |
| **Batch Operation TransactionRId View Field** | (Search) View field that supplies the record id per row. |
| **Batch Operation Source Unit** | (Child Unit) Child grid unit; loops child rows (must exist on the data model). |

Use batching when SQL, API, or messaging must repeat for many records (search result set, dataset export, each child line).

### 2.5 Notes

Designer-only **Notes** textarea. Not shown to end users at runtime.

### 2.1 Operation Basic Options — storage

| UI | Column / JSON | upsert_command |
|----|----------------|----------------|
| Operation Name | `Name` | `name` |
| Operation Type | `ActionType` | `actionType` (**required**, enum int) |
| Flow Order | `ActionFlowOrder` | `actionFlowOrder` / `optionsJson` |
| Execute Condition Field | `CommandConditionTransactionFieldId` | FK int or null |
| Is Public Operation | `ActionAttribute.LinkToUI` | `optionsJson.linkToUI` |
| Visible on Form Menu | `ActionAttribute.IsShowOnTopMenu` | `optionsJson.isShowOnTopMenu` |
| Visible on Calendar/… | `ActionAttribute.IsShowOnSearchViewEventOptionMenu` | `optionsJson.isShowOnSearchViewEventOptionMenu` |
| Auto Execute On Form Open | `ActionAttribute.IsAutoExecuteOnFormOpen` | `optionsJson.isAutoExecuteOnFormOpen` |

Category: **not stored**.

### 2.2 Operation Status Log — storage

| UI | JSON | Notes |
|----|------|-------|
| Log Operation Status | `EmAppCommandLoggingPreference` (editor only) | May not round-trip; use `IsLogCommandStartEnd` / `IsLogErrorDetails` in `optionsJson` |
| Log Operation Start & End | `IsLogCommandStartEnd` | Workflow root |
| Log Operation Info & Error Details | `IsLogErrorDetails` | Workflow root |

### 2.3 Operation Execution Result Display — storage

| UI | JSON |
|----|------|
| Execute Result Console Display Type | `EmAppValidationResultPreference` (1 details, 2 status only) |
| Enable customized success | `IsEnableCommandExecuteResultMessage` |
| Success text | `CommandSuccessMessage` |

`CommandFailedMessage` exists in DTO/runtime; **no React editor**.

### 2.4 Batch Operation Setting — storage

| UI | JSON |
|----|------|
| Is Batch Operation | `IsBatchCommand` |
| Batch Operation Source Type | `BatchCommandSourceFromType` (1 DataSet, 2 Search, 3 ChildUnit) |
| Batch Operation DataSet | `BatchCommandDataSetId` |
| TransactionRId DataSet Field | `BatchCommandSourceDataSetFieldName` |
| Batch Operation Search | `BatchCommandSearchId` |
| TransactionRId View Field | `BatchCommandSourceViewFieldId` |
| Criteria mapping | `DictBatchSearchCrietraIdAndTransFieldId` |
| Source Unit | `ForeachLoopSourceUnitId` |

### 2.5 Notes

| UI | Column |
|----|--------|
| Notes | `Description` (max 500) |

---



## 3. UI Operations

### 3.1 UI: Close Form Window

#### Purpose
Close the current master-detail form window or tab from the client when the user runs the operation.

#### When to use
After a successful Composition (save + message) as the **last** user-visible step, or as a standalone menu item *Close*.

#### Sections shown in the editor
Shared sections only (§2). No type-specific collapsible section.

#### Control reference
| Control | What it does |
|---------|----------------|
| *(none beyond §2)* | Menu visibility and conditions use shared controls only. |

#### Popups and special buttons
None.

#### Step-by-step configuration
1. Add operation; set **Operation Type** → **UI: Close Form Window**.
2. Set **Operation Name** (for example *Close*).
3. Turn on **Is Public Operation** and **Visible on Form Menu** if it should appear on the form menu.
4. Optionally place as the final child inside a **Composition** (see §9.1).

#### Pitfalls
- Running from the **server** inside Composition does not close the browser; the **client** must execute this type. As a composition child on the server path, it may not close the UI.
- Requires the form to be open in the client.

#### Agent metadata
| Item | Value |
|------|--------|
| **actionType** | **54** |
| **Display** | UI: Close Form Window |

#### Storage mapping
Operation-specific attributes: none. Uses §2 columns/JSON only.

#### `upsert_command` / `optionsJson` notes
- `actionType`: **54**
- Menu flags via `linkToUI`, `isShowOnTopMenu`, etc.

#### Runtime behavior
Client form menu (`FormMainMenus`): closes form tab/window. Server composition child does not close browser UI.

---

### 3.2 UI: Navigate To Search

#### Purpose
Open a linked **search** view from the form (navigation in the client).

#### When to use
User clicks *View related list* or *Open search* from the form.

#### Sections shown in the editor
1. Shared sections (§2).
2. **Navigate To Search** — field **Navigate To Search** (linked search picker).

#### Control reference
| Control | What it does |
|---------|----------------|
| **Navigate To Search** | Dropdown of searches linked on the root unit. Must be configured on the data model’s linked searches first. |

#### Popups and special buttons
None (dropdown only).

#### Step-by-step configuration
1. On the data model, link the target search to the root unit.
2. Add operation → **UI: Navigate To Search**.
3. Pick the search in **Navigate To Search**.
4. Public + **Visible on Form Menu** as needed.

#### Pitfalls
- If no linked searches exist, the dropdown is empty.
- FE-only when run from the form menu; server composition does not open searches in the browser.

#### Agent metadata
| **actionType** | **53** |

#### Storage mapping
`ActionAttribute.LinkedSearchId` (int).

#### `upsert_command` / `optionsJson` notes
- `actionType`: **53**
- `optionsJson.linkedSearchId` or attribute mirror from `get_command_detail`.

#### Runtime behavior
FE-only: opens linked search configured on root unit.

---

### 3.3 UI: Print

#### Purpose
Print the current form using the client print flow.

#### When to use
User needs a printable view of the **saved** record.

#### Sections shown in the editor
Shared sections only.

#### Control reference
| Control | What it does |
|---------|----------------|
| *(none beyond §2)* | |

#### Popups and special buttons
None.

#### Step-by-step configuration
1. Add operation → **UI: Print**.
2. Public + menu visibility.
3. Ensure users save before print (unsaved new records may lack data).

#### Pitfalls
- Requires a persisted root primary key for a meaningful print.

#### Agent metadata
| **actionType** | **48** |

#### Storage mapping
None beyond §2.

#### `upsert_command`
`actionType`: **48**

#### Runtime behavior
FE-only print; requires saved root PK.

---

### 3.4 UI: Print From Message Template

#### Purpose
Generate print output using a **message template** (server-side template render for master-detail).

#### When to use
Formatted printouts driven by template design rather than the default form print.

#### Sections shown in the editor
1. Shared sections (§2).
2. **Message Template** — **Message Template** picker.

#### Control reference
| Control | What it does |
|---------|----------------|
| **Message Template** | Select template; chevron opens template list popup; pencil opens template editor when selected. |

#### Popups and special buttons
Message template selector / editor popups (same pattern as other template pickers).

#### Step-by-step configuration
1. Create or pick a **Message Template** for this data model.
2. Add operation → **UI: Print From Message Template**.
3. Select **Message Template**.
4. Public + menu if user-facing.

#### Pitfalls
- Menu execution posts to server; ensure template is valid for the transaction.
- Saved record recommended.

#### Agent metadata
| **actionType** | **74** |

#### Storage mapping
`ActionAttribute.MessageTemplateId` (or template id field used by section).

#### `upsert_command`
`actionType`: **74**; set template id in attribute JSON.

#### Runtime behavior
Server invoked from menu; template render path (verify master-detail branch).

---

### 3.5 UI: Save As

#### Purpose
Start a **save as** / copy flow for the current form in the client.

#### When to use
User duplicates the record into a new form instance.

#### Sections shown in the editor
Shared sections only.

#### Control reference
| Control | What it does |
|---------|----------------|
| *(none beyond §2)* | |

#### Popups and special buttons
None on the operation editor (save-as UI opens at runtime).

#### Step-by-step configuration
1. Add operation → **UI: Save As**.
2. Name and menu flags.

#### Pitfalls
- Client menu path; composition on server may use different open-form flags (see Execute Data Transfer **Need To Open Form**).

#### Agent metadata
| **actionType** | **47** |

#### Storage mapping
None beyond §2.

#### Runtime behavior
FE save-as flow.

---

## 4. Backend — Form Refresh, Validate & Save

### 4.1 Save

#### Purpose
Persist form data (same semantic as the form Save action).

#### When to use
Menu Save, or a Composition step before SQL/messaging that needs committed data.

#### Sections shown in the editor
Shared sections only.

#### Control reference
| Control | What it does |
|---------|----------------|
| *(none beyond §2)* | |

#### Popups and special buttons
None.

#### Step-by-step configuration
1. Add **Save**; set Public + **Visible on Form Menu** for a toolbar/menu save.
2. Or add as **Private** child in Composition after Validate / formulas.

#### Pitfalls
- Form menu Save may run on client first; server Composition always persists via server save pipeline.
- Does not run formulas unless a separate **Exec DataModel All Formula** step exists.

#### Agent metadata
| **actionType** | **49** |

#### Runtime behavior
FE `handleSave` on menu; server `SaveTransactionData` in composition/batch/auto-exec.

---

### 4.2 Refresh

#### Purpose
Reload form data from the server (discard unsaved client changes unless saved).

#### When to use
After external updates, or as last step when child operations changed data.

#### Sections shown in the editor
Shared sections only.

#### Control reference
| Control | What it does |
|---------|----------------|
| *(none beyond §2)* | |

#### Popups and special buttons
None.

#### Step-by-step configuration
1. Add **Refresh**; menu or composition child.

#### Pitfalls
- Users lose unsaved edits when refreshing.

#### Agent metadata
| **actionType** | **50** |

#### Runtime behavior
FE refresh menu; server reload in composition/batch.

---

### 4.3 Validate Required Fields

#### Purpose
Server-side check that required fields have values; fails the operation if not.

#### When to use
**Before Save** in Composition (*validate → save → notify*).

#### Sections shown in the editor
Shared sections only.

#### Control reference
| Control | What it does |
|---------|----------------|
| *(none beyond §2)* | |

#### Popups and special buttons
None.

#### Step-by-step configuration
1. Add Private **Validate Required Fields** as first child in Composition.
2. Add **Save** after it.

#### Pitfalls
- Not in List Edit picker.
- Does not validate custom business rules beyond required-field metadata.

#### Agent metadata
| **actionType** | **205** |

#### Runtime behavior
`MasterDetailDataFirstLevelValidation` on server; omit from List Edit picker.

---

## 5. Backend — Form Data Processing

### 5.1 Exec DataModel All Formula

#### Purpose
Run **all** data-model formulas (calculated fields) for the transaction; marks data dirty but does **not** save.

#### When to use
Before Save when formulas must be up to date, or after DataLoad.

#### Sections shown in the editor
1. Shared sections (§2).
2. **Exec DataModel All Formula** — **Edit Data Model Formulas** shortcut.

#### Control reference
| Control | What it does |
|---------|----------------|
| **Edit Data Model Formulas** | **Click To Edit Formulas** opens Application Form Builder on **Transaction Formula Setting** for this data model (shortcut, not stored on the operation). |

#### Popups and special buttons
Application Form Builder popup for formula design.

#### Step-by-step configuration
1. Add **Exec DataModel All Formula**.
2. Optional: click **Click To Edit Formulas** to maintain formulas.
3. In Composition: place before **Save** if calculated fields must persist.

#### Pitfalls
- Without a following **Save**, users see updated values only in memory until they save manually.

#### Agent metadata
| **actionType** | **44** |

#### Runtime behavior
Runs all transaction formulas; dirty state; no save.

---

### 5.2 Execute Command Formula

#### Purpose
Run one **assignment formula** (command-level expression) against the form.

#### When to use
Targeted calculation not covered by standard field formulas.

#### Sections shown in the editor
1. Shared sections (§2).
2. **Execute Commnad Formula** — **Execute Formula** editor.

#### Control reference
| Control | What it does |
|---------|----------------|
| **Execute Formula** | Expression editor; must be non-empty. Uses form field tokens in expression syntax (same family as transaction formulas). |

#### Popups and special buttons
Formula editor surface (embedded code/expression UI).

#### Step-by-step configuration
1. Add **Execute Command Formula**.
2. Enter **Execute Formula**.
3. Typically Private in Composition before Save.

#### Pitfalls
- Empty formula fails validation / runtime.
- Display name retains typo *Commnad* in the UI.

#### Agent metadata
| **actionType** | **57** |

#### Storage mapping
`ActionAttribute.ExecutionFormula`, `ExecutionFormulaUI`.

#### `upsert_command`
Include `executionFormula` in attribute/`optionsJson`.

---

### 5.3 Execute All DataLoad

#### Purpose
Execute all configured **DataLoad** rules on the data model template.

#### When to use
Populate defaults or lookup-driven values when the form opens or on button click.

#### Sections shown in the editor
Shared sections only (no extra fields).

#### Control reference
| Control | What it does |
|---------|----------------|
| *(none beyond §2)* | DataLoad rules are defined on the data model, not on this operation row. |

#### Popups and special buttons
None.

#### Step-by-step configuration
1. Configure DataLoad on the data model.
2. Add **Execute All DataLoad** (often **Auto Execute On Form Open** with Public).

#### Pitfalls
- Does not save; follow with Save if values must persist immediately.

#### Agent metadata
| **actionType** | **45** |

---

### 5.4 Generate Matrix

#### Purpose
Generate matrix/grid data for a selected **matrix unit**.

#### When to use
Matrix-style child grids driven by matrix engine.

#### Sections shown in the editor
1. Shared sections (§2).
2. **Generate Matrix** — **Maxtri Unit** dropdown (UI label spelling).

#### Control reference
| Control | What it does |
|---------|----------------|
| **Maxtri Unit** | Child unit configured as matrix unit on this data model. |

#### Popups and special buttons
None.

#### Step-by-step configuration
1. Define matrix unit on data model.
2. Pick **Maxtri Unit**.
3. Run from menu or composition.

#### Pitfalls
- Wrong unit → empty or incorrect matrix.

#### Agent metadata
| **actionType** | **46** |

#### Storage mapping
`ActionAttribute.ParamId1` = matrix child unit id.

---

### 5.5 Execute SQL Statement

#### Purpose

Run T-SQL against the application database using values from the open form (and optional batch/search context). Supports `UPDATE`, `INSERT`, `DELETE`, scalar `SELECT`, and scripts with `DECLARE`.

#### When to use

- Persist calculated values or status flags in tables outside the form’s normal save path.
- Run set-based updates that reference form field values.
- Read a single scalar from SQL and write it into a root form field.

#### Sections shown in the editor

1. Shared sections (§2).
2. Collapsible **SQL Statement**.

#### Assign Query Scalar Result To Root Field

| Control | What it does |
|---------|----------------|
| **Assign Query Scalar Result To Root Field** | Dropdown of root fields. Use when SQL returns **one scalar** (for example `SELECT COUNT(*)` or one column). Choose `(None)` for statements that do not return a value to assign. |

| Control | What it does |
|---------|----------------|
| **SQL Statement** | Monaco SQL editor on the main form. Tokens like `[TF_####_FieldName]` are replaced at run time. |

#### Query Builder with Built-in Tokens

Open via the **Query Builder with Built-in Tokens** button beside **SQL Statement**.

| Control | What it does |
|---------|----------------|
| Left SQL editor | Same SQL as the main field; more space. Cursor position receives inserted tokens. |
| **Query Design Tool** | Opens **Operation Query Builder** (metadata/query designer). Seeds from **current editor selection** if non-empty; otherwise full SQL in the popup. Built SQL is inserted at the selection when you confirm in the designer. |
| **Apply & Close** | Writes popup SQL back to the operation and closes. **Always use this when finished.** |
| **Close** (X) | Closes without applying popup edits (main editor keeps previous saved text). |
| Full screen control | Expands the popup frame (EmbeddedLinkedPopupFrame). |
| **Example: Use Token In Query** | Shows sample `UPDATE` / `DECLARE` patterns using field tokens and `[CurrentUserId]`. |
| **Global Fields** | (When workflow/globals exist.) Click a field to insert its token at the cursor. |
| **Form Root Level Fields** | Click a field label (for example **Title**, **OpportunityId**) to insert `[TF_####_Title]`, `[TF_####_OpportunityId]`, etc. `####` is the internal field id. |
| **Grid {unit} Columns** | Child grid columns. Insert tokens for values tied to grid context when batch/child rules apply. |
| **Built-in Token** | Click to insert: `[CurrentUserId]`, `[CurrentPartnerId]`, `[CurrentUserName]`, `GETDATE()`, `GETUTCDATE()`. |

**Token click workflow:** Place the cursor in the SQL editor where the value belongs (for example after `WHERE OpportunityId = `), then click **OpportunityId** under **Form Root Level Fields**. The token `[TF_####_OpportunityId]` is inserted at the cursor.

#### Step-by-step configuration

1. **Add Operation** (Public or Private tab as appropriate).
2. Set **Operation Name** and **Operation Type** → **Execute SQL Statement**.
3. Configure **Execute Condition Field** / menu visibility if needed.
4. Open **Query Builder with Built-in Tokens**.
5. Draft SQL; use **Query Design Tool** if you need table/column discovery.
6. Insert tokens from **Form Root Level Fields**, **Grid** columns, or **Built-in Token** (do not hand-type field ids unless you must).
7. Click **Apply & Close**.
8. If SQL returns one value for the form, set **Assign Query Scalar Result To Root Field**.
9. Save the data model; test from the form Operation menu (avoid **Auto Execute On Form Open** until verified).
10. For many rows, enable **Is Batch Operation** and confirm tokens/ids match the batch key field.

#### Pitfalls

- Tokens must match live field ids; renaming fields can invalidate old tokens—re-click fields in the builder.
- Batch SQL runs **per source row**; verify which record id is in scope.
- Closing the builder with **X** without **Apply & Close** drops unsaved popup edits.
- `SELECT` without assign field updates nothing on the form.
- Composition child SQL runs on the server; UI-only types in the same composition do not run SQL for you.

#### Agent metadata
| **actionType** | **42** |

#### Storage mapping
- SQL: column **`NotificationMessage`** (NOT `ActionAttribute.SqlStatement`).
- Scalar: `ActionAttribute.AssignSqlResultToFiledId`.

#### `upsert_command` / `optionsJson` notes
- **Always** put SQL in `notificationMessage` when `actionType`=42.
- `assignSqlResultToFiledId` in attributes.
- Tokens: resolve `sqlToken` from `find_fields` / `get_unit_fields` / `rootPrimaryKeys` — format `[TF_{fieldId}_{dbColumn}]`.
- Builtins only: `[CurrentUserId]`, `[CurrentPartnerId]`, `[CurrentUserName]`, `GETDATE()`, `GETUTCDATE()`.
- **Good:** `UPDATE Crm126_Opportunity SET Notes = Notes + 'ABC' WHERE OpportunityId = [TF_3499_OpportunityId]`
- **Bad:** no `WHERE`; `WHERE OpportunityId = [rootid]`; inventing tokens without calling field tools.
- Preview may return `warnings` — fix before `confirmed=true`.

#### Runtime behavior
Token replace → execute SQL → optional scalar to root field.

---

### 5.6 Execute Data Transfer

#### Purpose
Run a configured **Data Transfer** mapping (same family as integration data transfer).

#### When to use
Push/pull data to another data model or external mapping after save or on button.

#### Sections shown in the editor
1. Shared sections (§2).
2. **Execute Data Transfer**.

#### Control reference
| Control | What it does |
|---------|----------------|
| **Data Transfer Setting** | Picker for transfer definition; chevron opens list; pencil opens editor. |
| **Execute Task On Target Data Model** | Optional target **operation** on the destination data model to run after transfer. |
| **Need To Open Form** | Checkbox — open target form after transfer when appropriate. |

#### Popups and special buttons
Data Transfer setting selector and editor popups.

#### Step-by-step configuration
1. Create **Data Transfer Setting**.
2. Add **Execute Data Transfer**; pick setting.
3. Optionally set target operation and **Need To Open Form**.

#### Pitfalls
- Available on **List Edit** models (short picker list).
- Target operation must exist on destination data model.

#### Agent metadata
| **actionType** | **43** |

#### Storage mapping
`DataTransferSettingId`, `TargetTransactionCommandId`, `IsNeedToOpenNewForm`.

#### Runtime
List Edit allowed type; transfer BL + optional target operation.

---

## 6. Backend — Import & File Generation

Types in this group share collapsible **File / Import Settings** (paths, import setting pickers, FTP when needed).

### 6.1 Import from Json

#### Purpose
Import JSON file content into database table(s) using a **Json import setting**.

#### When to use
One known JSON file path per run.

#### Sections shown
§2 + **File / Import Settings** — import setting picker, **File Path** (with examples: local, http, ftp).

#### Control reference
| Control | What it does |
|---------|----------------|
| **Json Import Setting** | Named import map; Confirm Selection & Close in selector popup. |
| **File Path** | Source file; tokens allowed; examples shown in UI. |

#### Popups and special buttons
Import setting management popup (*Please select an import setting, and then click "Confirm Selection & Close"*).

#### Step-by-step configuration
1. Create Json import setting in DB management.
2. Add operation; pick setting and **File Path**.
3. Test with Private composition first.

#### Pitfalls
- Wrong setting → wrong table mapping.
- Path must be reachable from server.

#### Agent metadata
| **actionType** | **66** |
#### Storage
`JsonImportSettingId`, `FilePath` on attribute + path fields.
#### upsert_command
`jsonImportSettingId`, `filePath`.

---

### 6.2 Import from Excel

#### Purpose
Import one Excel file via **Excel import setting**.

#### When to use
Single `.xlsx` / similar path per run.

#### Sections shown
§2 + **File / Import Settings**.

#### Control reference
| Control | What it does |
|---------|----------------|
| **Excel Import Setting** | Import map picker. |
| **File Path** | Excel file path (folder or file URL patterns per examples). |

#### Popups and special buttons
Excel import selector / editor.

#### Step-by-step configuration
Same pattern as §6.1 with Excel setting.

#### Pitfalls
- File lock if Excel open on server path.

#### Agent metadata
| **actionType** | **67** |
`ExcelImportSettingId`, `FilePath`.

---

### 6.3 Import from Rest-Api setting

#### Purpose
Import using a pre-built **Rest-Api Import Setting**.

#### When to use
REST pull configured in integration module.

#### Sections shown
§2 + **File / Import Settings** — Rest-Api setting picker only (no file path).

#### Control reference
| Control | What it does |
|---------|----------------|
| **Rest-Api Import Setting** | Picker + editor shortcuts. |

#### Popups and special buttons
Rest-Api import management popup.

#### Step-by-step configuration
1. Configure Rest-Api import.
2. Pick setting on operation.

#### Pitfalls
- Credentials and URL live in the import setting, not on the operation.

#### Agent metadata
| **actionType** | **71** |
`RestApiDbImportSettingId`.

---

### 6.4 Import from DB-To-DB setting

#### Purpose
Import/copy using **DB-To-DB Import Setting**.

#### When to use
Server-side DB pipeline already defined.

#### Sections shown
§2 + **File / Import Settings**.

#### Control reference
| Control | What it does |
|---------|----------------|
| **DB-To-DB Import Setting** | Picker. |

#### Popups and special buttons
DB-To-DB management / editor.

#### Step-by-step configuration
Pick setting; run from menu or composition.

#### Pitfalls
- Connection errors surface at runtime in execution result.

#### Agent metadata
| **actionType** | **72** |
`DbToDbImportSettingId`.

---

### 6.5 Multi Json files in folder

#### Purpose
Import **many** JSON files under a folder (and subfolders); file names must match import settings.

#### When to use
Batch drop folder processing.

#### Sections shown
§2 + **File / Import Settings** — **File Path** = root folder.

#### Control reference
| Control | What it does |
|---------|----------------|
| **File Path** | Root folder (example `c:\MyJsonFileFolder\`). |

#### Popups and special buttons
None beyond path entry.

#### Step-by-step configuration
1. Ensure one import setting per file naming pattern.
2. Set folder path; test small folder first.

#### Pitfalls
- Unmatched file names are skipped or error depending on server rules.

#### Agent metadata
| **actionType** | **76** |
`FilePath` = folder root.

---

### 6.6 Multi Excel files in folder

#### Purpose
Same as §6.5 for Excel files in a folder tree.

#### Sections shown
§2 + **File Path** folder.

#### Control reference
| Control | What it does |
|---------|----------------|
| **File Path** | Root folder for Excel files. |

#### Popups and special buttons
None.

#### Step-by-step configuration
Set folder; verify settings cover each file pattern.

#### Pitfalls
- Large folders → long run; consider batch operation logging.

#### Agent metadata
| **actionType** | **77** |

---

### 6.7 Download File To Server Folder

#### Purpose
Download/copy a remote or local source file to a **server destination** path (optional FTP credentials section).

#### When to use
Pull attachment or feed file onto app server disk.

#### Sections shown
§2 + **File / Import Settings** — source **File Path**, **Distination File Path**, FTP fields when applicable.

#### Control reference
| Control | What it does |
|---------|----------------|
| **File Path** | Source (http, ftp, local). |
| **Distination File Path** | Server folder or full target path (UI spelling *Distination*). |
| FTP user/password | Shown when path requires FTP auth. |

#### Popups and special buttons
FTP/path helper section.

#### Step-by-step configuration
1. Set source and destination paths (see in-editor examples).
2. Run with service account permissions in mind.

#### Pitfalls
- Typo *Distination* in label; map mentally to destination.
- Overwrite behavior depends on server implementation.

#### Agent metadata
| **actionType** | **73** |
`FilePath`, `DistinationFilePath`, FTP credential fields when used.

---

### 6.8 Convert Xml to Json

#### Purpose
Read XML from **File Path**, write JSON to **Distination File Path**.

#### When to use
Integration bridge files.

#### Sections shown
§2 + source/destination paths + examples (http, ftp, local).

#### Control reference
| Control | What it does |
|---------|----------------|
| **File Path** | Source `.xml`. |
| **Distination File Path** | Target `.json` or folder. |

#### Popups and special buttons
None.

#### Step-by-step configuration
Enter paths; test with small sample file.

#### Pitfalls
- Invalid XML fails the step.

#### Agent metadata
| **actionType** | **81** |

---

### 6.9 Convert Json to Xml

#### Purpose
Inverse of §6.8 — JSON source to XML destination.

#### Sections shown
§2 + paths.

#### Control reference
| Control | What it does |
|---------|----------------|
| **File Path** | Source `.json`. |
| **Distination File Path** | Target `.xml`. |

#### Popups and special buttons
None.

#### Step-by-step configuration
Same as §6.8 with reversed formats.

#### Pitfalls
- Schema expectations on downstream consumer.

#### Agent metadata
| **actionType** | **82** |

---

## 7. API, Plugin & External Process

### 7.1 Call API Operation

#### Purpose
Invoke configured **API Operation** / data transfer mapping with optional logging flags.

#### When to use
Call external REST/SOAP style integration defined in API Operation library.

#### Sections shown
§2 + **Call API Operation**.

#### Control reference
| Control | What it does |
|---------|----------------|
| **API Call Log Setting** | Checkboxes for what to log (request/response/detail per UI). |
| **API Operation** | Primary mapping picker; opens embedded mapping editor when selected. |

#### Popups and special buttons
API Operation selector and embedded transfer/mapping editor.

#### Step-by-step configuration
1. Define API Operation mapping.
2. Pick on operation; tune log checkboxes.
3. Map path/query params in embedded editor if shown.

#### Pitfalls
- Wrong mapping id → wrong endpoint.
- Timeouts appear as execution errors.

#### Agent metadata
| **actionType** | **59** |
`DataTransferSettingId` (API mapping), API log flags in section.
Runtime: `CallApiOperationFromMasterDetailTransactionForm`.

---

### 7.2 Call Plugin Web Api

#### Purpose
Call a named method on the **plugin** Web API bridge.

#### When to use
Custom plugin assemblies expose `WebApiMethodName`.

#### Sections shown
§2 + **Call Plugin Web Api** — **API Method** text.

#### Control reference
| Control | What it does |
|---------|----------------|
| **API Method** | Plugin method name string. |

#### Popups and special buttons
None.

#### Step-by-step configuration
Enter exact plugin method name; deploy plugin on server.

#### Pitfalls
- Method name mismatch → runtime error.

#### Agent metadata
| **actionType** | **40** |
`WebApiMethodName`.

---

### 7.3 External Method MasterDetail

#### Purpose
Invoke registered **external method** (.NET registration) with master-detail context.

#### When to use
Advanced custom code registered in External Method Register.

#### Sections shown
§2 + **External Method MasterDetail** — method dropdown.

#### Control reference
| Control | What it does |
|---------|----------------|
| **External Method** | Pick from registered methods list (loaded from admin API). |

#### Popups and special buttons
None.

#### Step-by-step configuration
Register method; pick in dropdown.

#### Pitfalls
- Assembly must be deployed and registered in target environment.

#### Agent metadata
| **actionType** | **38** |
`ExternalMethodRegisterId`.

---

### 7.4 Execute External Exe Process

#### Purpose
Start an external **executable** on the server with arguments.

#### When to use
Legacy EXE tools, command-line utilities (use with security caution).

#### Sections shown
§2 + **File / Import Settings** — **File Path** (exe), **Arguments** (JSON/editor).

#### Control reference
| Control | What it does |
|---------|----------------|
| **File Path** | Full path to `.exe` (example `c:\FolderPath\filename.exe`). |
| **Arguments** | Argument string / JSON per editor. |

#### Popups and special buttons
None.

#### Step-by-step configuration
1. Ensure exe allowed on server.
2. Set path and arguments; test Private first.

#### Pitfalls
- Security: arbitrary exe execution.
- Process may outlive operation unless server waits.

#### Agent metadata
| **actionType** | **68** |
`FilePath` (exe), `Arguments`.

---

## 8. Messaging

### 8.1 Shared notification fields

Messaging types show collapsible **Notification Message** (types 60–63).

| Control | What it does |
|---------|----------------|
| **Use Rich Text Message Template** | Toggle rich template mode. |
| **Notification Subject** | Subject line (email/partner/user). |
| **Notification Message** | Body text or template body; supports field tokens where editor offers insert helpers. |
| **Message Template** | Optional template picker when using template mode. |
| **Attach Form Files** | Attach all form files to message when enabled. |

### 8.2 Message UserId

#### Purpose
Send in-app style message to user id taken from a form field.

#### Sections shown
§2 + **Notification Message** + **Message To UserId Field**.

#### Control reference
| Control | What it does |
|---------|----------------|
| **Message To UserId Field** | Any field holding user id. |

#### Step-by-step configuration
Pick destination field; compose subject/body; Public or Composition child.

#### Pitfalls
- Invalid user id → send failure.

#### Agent metadata
| **actionType** | **60** |
`NotificationDestinationPartnerIdTransactionFiledId` (UserId reuse); subject/body in notification fields + `NotificationMessage` column as applicable.

---

### 8.3 Email

#### Purpose
Email to address in **Email Address Field**; optional **Message Link Operations**.

#### Sections shown
§2 + **Notification Message** + **Message Link Operations** grid.

#### Control reference
| Control | What it does |
|---------|----------------|
| **Email Address Field** | Root/transaction field with email. |
| **Message Link Operations** | Child operations embedded as links in email (see §8.6). |

#### Step-by-step configuration
1. Set email field and message content.
2. Add Private children under **Message Link Operations** if recipients should run operations from the email.

#### Pitfalls
- Links run later on recipient click, not at send time.

#### Agent metadata
| **actionType** | **61** |
`NotificationDestinationEmailAddressTransactionFiledId`; `ChildActionList` for Message Link Operations.

---

### 8.4 Partner

#### Purpose
Message to **partner** from **Message To Partner Field**; optional **Aditional To Email Address** CC.

#### Sections shown
§2 + destination fields + optional **Aditional To Email Address** + **Message Link Operations**.

#### Control reference
| Control | What it does |
|---------|----------------|
| **Message To Partner Field** | Partner id field. |
| **Aditional To Email Address** | Extra email recipients (UI spelling *Aditional*). |
| **Message Link Operations** | Same as Email. |

#### Step-by-step configuration
Pick partner field; optional CC; compose notification.

#### Pitfalls
- Partner must resolve to deliverable channel.

#### Agent metadata
| **actionType** | **62** |
Partner field + `ToEmailAddress`; message links via `ChildActionList`.

---

### 8.5 SMS

#### Purpose
SMS to **Phone Number Field**; body primarily in **Notification Message**.

#### Sections shown
§2 + **Phone Number Field** + notification fields.

#### Control reference
| Control | What it does |
|---------|----------------|
| **Phone Number Field** | Mobile number field. |

#### Step-by-step configuration
Select phone field; keep SMS body concise in **Notification Message**.

#### Pitfalls
- SMS gateway configuration is environment-level.

#### Agent metadata
| **actionType** | **63** |
`SmsMessageToPhoneNumberFiledId`; SMS body in `NotificationMessage`.

---

### 8.6 Message Link Operations

#### Purpose
Define **child operations** whose links are embedded in Email/Partner messages.

#### When to use
Approval links, “complete task” links.

#### Sections shown
Under Email (§8.3) or Partner (§8.4): **Message Link Operations** grid.

#### Control reference
| Column / control | What it does |
|------------------|----------------|
| Add child | Adds Private operations as link targets. |
| **Is Batch Operation** (read-only column) | Shows if child is batch. |
| Grid row actions | Edit child in popup editor. |

#### Popups and special buttons
**Child Command Editor** popup (same family as Composition children).

#### Step-by-step configuration
1. Configure parent Email/Partner operation.
2. In **Message Link Operations**, add Private children (for example Approve Composition).
3. Send message; recipient clicks link to execute child pipeline.

#### Pitfalls
- Skip/switch conditions on link children rarely used until link execution.
- Children should be Private.

---

## 9. Composition

### 9.1 Composition Operation

#### Purpose
Run an ordered list of **Child Operations** with optional switch field, skip, continue-on-error, external data model children.

#### When to use
One menu item runs validate → formula → save → SQL → email → close.

#### Sections shown
§2 (Category hidden) + **Composition Operation**.

#### Control reference
| Control | What it does |
|---------|----------------|
| **Child Operations Switch Condition Field** | Root dropdown/list field; enables per-child **Child Task Switch Condition** values in grid. |
| **Child Operations** grid | Columns: order (**Sort**), operation name, skip flags, continue on error, batch flag (read-only), switch condition, change-trigger fields, external data model, etc. |
| **Add** dropdown | Add existing Private operation, new internal child, or external data model child. |

#### Popups and special buttons
- **Available Child Operations** selector.
- **Child Command Editor** popup.
- External **Application Form Builder** when editing external model child.

#### Step-by-step configuration
1. Set **Operation Type** → **Composition Operation** (or pick Composition in tree).
2. Create Private children (Validate, Save, SQL, …) in the left list first.
3. In **Child Operations**, add children in order; set **Sort**.
4. Optional: set **Child Operations Switch Condition Field** and per-row switch values.
5. Parent Composition is usually **Public** + menu; children **Private**.

#### Pitfalls
- UI Close/Navigate children do not run on pure server workflow the same as client menu.
- **Go To Next With Error** changes failure handling—use deliberately.
- External children require permissions on other data models.

#### Agent metadata
| **actionType** | **200** |

#### Storage mapping
`ActionAttribute.ChildActionList[]`, `ChildCommandsSwitchConditionFieldId`.

#### `upsert_command`
- `actionType`: **200**
- **Required workflow (same as UI):**
  1. Create each child as its own Private operation (`linkToUI: false`) — e.g. SQL child `actionType` 42 + `notificationMessage`.
  2. `get_commands` / note each child’s `commandId`.
  3. Upsert the Composition with `optionsJson`:
     `{"linkToUI":true,"isShowOnTopMenu":true,"childActionList":[{"commandId":123,"sort":1},{"commandId":456,"sort":2}]}`
  4. `request_form_reload` — Child Operations grid binds `ActionAttribute.ChildActionList`.
- **Wrong:** create only a 200 shell (or put SQL on the composition’s `notificationMessage`) → UI Child list stays **empty**.
- Children normally `linkToUI: false`. Parent usually Public + menu.

---



## 10. Composition & Message Link children (agent)

`ChildActionList[]`: `Sort`, `CommandId`, `CommandDisplay`, `ExternalTransactionId`, `IsSkip`, `IsGoToNextCommandWithError`, `PredictValue`, `ChangeTriggerRootLevelFieldId`, `ChangeTriggerChildGridUnitId`.

Message link children: types **61**, **62**; links encrypted at send; executed when recipient clicks (not at send).

## 11. Execution pipeline

1. Form menu: FE-only types 47, 48, 49, 50, 53, 54 → client; others POST execute.
2. Server `ExecuteTransactionRootCommand`: condition → batch loop → composition or single executor.
3. Auto on open: first `LinkToUI && IsAutoExecuteOnFormOpen`.
4. List Edit server: strong support for 43, 49, 50.

## 12. `upsert_command` checklist

1. Read this KB + `get_command_detail` shape.
2. Correct **actionType** (`EmAppTransactionCommandType`), never menu link targets 1/2.
3. SQL (**42**): `notificationMessage` column.
4. SMS (**63**) / notifications: often `notificationMessage` for body.
5. Public flags: `optionsJson` → `linkToUI`, `isShowOnTopMenu`, `isAutoExecuteOnFormOpen`, `actionFlowOrder`.
6. Preview → `ask_user` → `confirmed=true` → `request_form_reload`.

## 13. Scenarios (I want to…) — agent

| Goal | actionType / pattern |
|------|----------------------|
| Menu save | 49 + `linkToUI` + `isShowOnTopMenu` |
| Validate → save → email | 200 composition children 205, 49, 61 (private) |
| SQL on OpportunityId | 42 + `notificationMessage` with `[TF_*_OpportunityId]` token |
| Batch search update | parent `isBatchCommand` + child 42 or 59 |

## 14. Good practices

- Mirror `get_command_detail` property casing in JSON patches.
- Never run operations from Configurator agent.
- After `upsert_command`, rely on `request_form_reload` for UI refresh.

## Change log

| Date | Note |
|------|------|
| 2026-11-17 | Full UI depth (mirrors User Help §3–9) + per-type actionType/storage/upsert. |
