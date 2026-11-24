# Data Model Operation — Agent Knowledge Base

Audience: `app-configurator` agent and developers. Not in-app user Help.  
Canonical path: `Document/KnowledgeBase/AgentKnowledge/app-configurator/commands-knowledge.md`  
User Help: `Document/KnowledgeBase/UserGuide/DataModelOperation-Help.md` → `AppReact/public/help/commands-user-help.md`  
Design plan: `Document/AgentDesign/AppConfigurator-Commands-Knowledge-Plan.md`  
Read before: `upsert_command`, `get_command_detail`.  
Sync: `Document/KnowledgeBase/SyncRuntimeDocs.ps1`

`actionType` is `EmAppTransactionCommandType` (int). Not link-target codes (1=Edit, 2=Create).

### Field tokens (Query Builder / Message / Formula) — hard rules

The Operation editor Query Builder with Built-in Tokens popup shows unit-layered field buttons (Form Root Level Fields, Grid `{unit}` Columns, builtins). Clicking a button inserts the live token at the cursor. Agents must emit the same strings — resolve via tools, never invent.

| Surface | Tool fields | Format |
|---------|-------------|--------|
| **Execute SQL (42)** | `sqlToken` | `[TF_{fieldId}_{dbColumn}]` e.g. `[TF_3499_OpportunityId]` |
| **Message / notification** | `messageToken` (same as `sqlToken` for fields) | `[TF_…]`; builtins include `[CurrentPkValue]`, `[Today]`, … |
| **Command Formula (57)** | `formulaToken` | Root: `[DbColumn_fieldId]`; grid/sibling: `[Table.DbColumn_fieldId]` |

Tools: `get_current_transaction_config` (includes `rootPrimaryKeys[]`), `find_fields`, `get_unit_fields` — each field has `sqlToken`, `messageToken`, `formulaToken`, `isPrimaryKey`, `unitKind` (`rootMaster` | `masterSibling` | `childGrid`).

Current form row / root master id: use `rootPrimaryKeys[].sqlToken` (fields with `isPrimaryKey` on `unitKind=rootMaster`). That is the Query Builder OpportunityId (etc.) button — not `[rootid]`.

SQL hard rules (42): SQL goes in `notificationMessage`. `UPDATE`/`DELETE` must `WHERE` the intended row(s); for "this form record" use root PK `sqlToken`. Forbidden: `[rootid]`, missing `WHERE`, hand-waved placeholders.

Command Formula hard rules (57):
1) Expression goes in `optionsJson.executionFormulaUI` → `ActionAttribute.ExecutionFormulaUI` (persisted inside ActionAttribute JSON / `FormulaExpression` column). **Not** `notificationMessage`.
2) Left side = `formulaToken` from `find_fields` / `get_unit_fields` (e.g. `[Notes_3499]`). Never bare `Notes`, never `[TF_…]`.
3) Datetime = `[Now]` or `[Today]` (not `Format(Now(), "…")`, not `Now()`).
4) Example: `[Notes_3499] = [Now]`. Do **not** use `update_unit_formula` for Command Formula (that tool is Calculation Flow / unit formulas).

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

Excluded from new picker: 51, 55, 70.

---

# Data Model Operation — Manual

## How to read this manual

Each operation chapter is ordered for doing first, looking up second: Purpose → When to use → Step-by-step → Editor sections → How-to notes (complex types only) → Control reference → Pitfalls.

Shared settings apply to every operation. Scenarios map goals to types.

---

## 1. Introduction

Same product terms as User Help. Storage table: `AppProjectWorkFlowAction`.  
Attribute JSON: `FormulaExpression` column deserialized to `AppActionAttributeDto`.  
Column `NotificationMessage`: SQL text (type 42), SMS body (63), general notification body for other types.

Agent workflow: `get_commands` → `get_command_detail` → read this KB → `upsert_command` with preview → `ask_user` → `confirmed=true` → `request_form_reload`. Never execute runtime operations from Configurator.

A Data Model Operation (short: Operation) is a configured action on a data model: save, refresh, run SQL, import files, send messages, call APIs, or run an ordered sequence.

- Public operations can appear on form or view menus, or run when the form opens.
- Private operations are steps inside Composition or Message Link Operations.

| Term | Meaning |
|------|---------|
| **Operation** | One row in the data model’s operation list (Save, Execute SQL Statement, …). |
| **Operation Type** | What the operation does. Chosen from the searchable tree; Category is guidance only. |
| **Public / Private** | Public → menus / auto-open eligible. Private → child steps. |
| **Composition** | Operation type that runs Child Operations in order. |
| **Batch** | Re-runs the operation per DataSet row, Search row, or child grid row. |

Where you edit: Application Form Builder → Data Model Operation (left list + right editor with collapsible sections).

Contexts:

- List Edit data models offer a short type list (Save, Refresh, Execute Data Transfer, Composition).
- Workflow Automation embedded editor omits pure UI window types (Print, Close, Navigate, Save As, Print From Template).

Excluded from new picks: UI: Open Form Creation/Edit Window, Create User (legacy rows may still exist).

Operation Type help: Click the ? next to Operation Type to open this manual at the current type.

---

## 2. Shared settings (every operation)

Collapsible sections appear in the right-hand Operation editor when you select an operation in the left list.

Typical setup (every new operation)

1. Add Operation on the Public or Private tab.
2. Set Operation Name and Operation Type.
3. For a menu button: turn on Is Public Operation and Visible on Form Menu.
4. Optionally set Execute Condition Field, Flow Order, result display, and Notes.
5. Only enable Is Batch Operation when the action must run once per source row (see Batch Operation Setting).
6. Save the data model and test from the form Operation menu before Auto Execute On Form Open.

---

### 2.1 Operation Basic Options

#### Purpose
Identify the operation, choose its type, and control when it appears or runs.

#### When to use
Every operation. Expand Operation Basic Options at the top of the editor.

#### Step-by-step configuration
1. Enter a short, user-facing Operation Name (for example *Submit Opportunity*).
2. Open Operation Type, search or browse the tree, and select a leaf (or Composition).
3. Set Flow Order if menu/list order matters.
4. Optionally pick Execute Condition Field (root checkbox).
5. For Public: set menu visibility and optional Auto Execute On Form Open.

#### Editor sections
Operation Basic Options (always available).

#### Control reference

| Control | What it does |
|---------|----------------|
| **Operation Name** | Label on the form Operation menu and in the operation list. |
| **Operation Type** | Searchable tree picker. Changing type shows or hides sections below; old type-specific values are not cleared automatically. |
| **Category** | Read-only breadcrumb from Operation Type (for example `Backend Operations / Form Data Processing`). Not saved. Hidden for Composition. |
| **Flow Order** | Numeric sort order in the list and on the form menu (lower first). Hidden in Workflow Automation embedded editor. |
| **Execute Condition Field** | Optional root checkbox. `No Condition (The task will execute)` = always run. If false at run time, menu item is disabled and the step is skipped (server composition/batch). |
| **Is Public Operation (Link To UI)** | On → menus / auto-run eligible. Off → internal (private) step. New rows inherit the active Public/Private tab. |
| **Visible on Form Menu** | Public only. Adds the operation to the form Operation menu. |
| **Visible on Calendar/Scheduler/Gantt View Menu** | Public only. Adds to calendar/Gantt/scheduler event menus. |
| **Auto Execute On Form Open** | Public only. Runs once when the form opens (first matching public operation with this flag). |

#### Pitfalls
- Changing Operation Type leaves stale values in hidden sections—review them.
- Only one public auto-on-open operation should be relied on; test before enabling.

---

### 2.2 Operation Status Log

#### Purpose
Control workflow automation start/end and detail logging.

#### When to use
Debugging workflow automation root operations. Leave off for ordinary form operations.

#### Step-by-step configuration
1. Expand Operation Status Log.
2. Choose Log Operation Status preference, or leave *Do Not Log*.

#### Editor sections
Operation Status Log.

#### Control reference

| Control | What it does |
|---------|----------------|
| **Log Operation Status** | Preference: *Log Result Details*, *Log Result Status Only*, or *Do Not Log*. Drives the two checkboxes (reopen may show *Do Not Log* while flags remain). |
| **Log Operation Start & End** | Workflow root start/end tracking. Disabled when preference is *Do Not Log*. |
| **Log Operation Info & Error Details** | Extra error detail for workflow roots. Disabled when preference is *Do Not Log*. |

#### Pitfalls
- Logging preference may not round-trip exactly after save; verify flags if debugging.

---

### 2.3 Operation Execution Result Display

#### Purpose
Control how success/failure messages appear to the user.

#### When to use
Tune console detail while developing; shorten for production if needed.

#### Step-by-step configuration
1. Expand Operation Execution Result Display.
2. Choose Execute Result Console Display Type.
3. Optionally enable a custom success message.

#### Editor sections
Operation Execution Result Display.

#### Control reference

| Control | What it does |
|---------|----------------|
| **Execute Result Console Display Type** | *Show Result Details* = longer console text. *Show Result Status Only* = brief status (failures may show only *Failed to Execute Operation.*). |
| **Customized Execution Result Message → Enable** | Shows a text area for a custom success message. |
| Success message text area | Plain text on success (token replacement when supported). |

#### Pitfalls
- Custom message is for success only; failure text is not edited here.

---

### 2.4 Batch Operation Setting

#### Purpose
Run the operation once per row from a DataSet, Search, or child grid instead of once for the open form.

#### When to use
SQL, API, or messaging must repeat for many records (search selection, dataset rows, each child line).

#### Step-by-step configuration
1. Expand Batch Operation Setting and turn on Is Batch Operation.
2. Choose Batch Operation Source Type: DataSet, Search, or Child Unit.
3. Pick the source (dataset / search / child unit) and the record id field for that source.
4. For Search, map criteria if the Batch Search Criteria Mapping popup is needed.
5. Confirm type-specific tokens (for example SQL) use the id that matches the batch key.
6. Test with a small source set first.

Example: Update every selected search row — set Source Search, pick the search and TransactionRId view field, then add Execute SQL Statement (or put this batch parent around a Composition that contains SQL).

#### Editor sections
Batch Operation Setting.

#### How-to notes
- Batch loops per source row; each iteration loads that record’s context.
- Child Unit batch requires the child grid unit on the data model.
- Prefer verifying ids in Query Builder / field pickers rather than hand-typed placeholders.

#### Control reference

| Control | What it does |
|---------|----------------|
| **Is Batch Operation** | On → once per source row; off → single open form. |
| **Batch Operation Source Type** | DataSet, Search, or Child Unit. |
| **Batch Operation DataSet** | (DataSet) Dataset picker; chevron = list; pencil = editor. |
| **Batch Operation TransactionRId DataSet Field** | (DataSet) Column holding the record id per iteration. |
| **Batch Operation Search** | (Search) Search view picker; optional criteria mapping popup. |
| **Batch Operation TransactionRId View Field** | (Search) View field that supplies the record id per row. |
| **Batch Operation Source Unit** | (Child Unit) Child grid unit to loop. |

#### Pitfalls
- Wrong id field → updates the wrong rows or none.
- Large search/dataset sources can run a long time; watch logging and timeouts.

---

### 2.5 Notes

#### Purpose
Designer-only documentation for this operation.

#### When to use
Record intent for other designers (not shown to end users).

#### Step-by-step configuration
1. Expand Notes.
2. Enter free text (keep concise).

#### Editor sections
Notes.

#### Control reference

| Control | What it does |
|---------|----------------|
| **Notes** | Designer-only textarea. |

#### Pitfalls
- Not visible at runtime to end users.

---

### 2.1 Operation Basic Options — storage

| UI | Column / JSON | upsert_command |
|----|----------------|----------------|
| Operation Name | `Name` | `name` |
| Operation Type | `ActionType` | `actionType` (required, enum int) |
| Flow Order | `ActionFlowOrder` | `actionFlowOrder` / `optionsJson` |
| Execute Condition Field | `CommandConditionTransactionFieldId` | FK int or null |
| Is Public Operation | `ActionAttribute.LinkToUI` | `optionsJson.linkToUI` |
| Visible on Form Menu | `ActionAttribute.IsShowOnTopMenu` | `optionsJson.isShowOnTopMenu` |
| Visible on Calendar/… | `ActionAttribute.IsShowOnSearchViewEventOptionMenu` | `optionsJson.isShowOnSearchViewEventOptionMenu` |
| Auto Execute On Form Open | `ActionAttribute.IsAutoExecuteOnFormOpen` | `optionsJson.isAutoExecuteOnFormOpen` |

Category: not stored.

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

`CommandFailedMessage` exists in DTO/runtime; no React editor.

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
After a successful Composition (save + message) as the last user-visible step, or as a standalone menu item *Close*.

#### Step-by-step configuration
1. Add operation; set Operation Type → UI: Close Form Window.
2. Set Operation Name (for example *Close*).
3. Turn on Is Public Operation and Visible on Form Menu if it should appear on the form menu.
4. Optionally place as the final child inside a Composition (see Composition Operation).

#### Editor sections
Shared settings only. No type-specific collapsible section.

#### Control reference

| Control | What it does |
|---------|----------------|
| *(none beyond Shared settings)* | Menu visibility and conditions use shared controls only. |

#### Pitfalls
- Running from the server inside Composition does not close the browser; the client must execute this type.
- Requires the form to be open in the client.


#### Agent metadata
| Item | Value |
|------|--------|
| **actionType** | 54 |
| **Display** | UI: Close Form Window |

#### Storage mapping
Operation-specific attributes: none. Uses Shared settings columns/JSON only.

#### `upsert_command` / `optionsJson` notes
- `actionType`: 54
- Menu flags via `linkToUI`, `isShowOnTopMenu`, etc.

#### Runtime behavior
Client form menu (`FormMainMenus`): closes form tab/window. Server composition child does not close browser UI.

---

### 3.2 UI: Navigate To Search

#### Purpose
Open a linked search view from the form (navigation in the client).

#### When to use
User clicks *View related list* or *Open search* from the form.

#### Step-by-step configuration
1. On the data model, link the target search to the root unit (Unit Navigate To Search / Linked Search management).
2. Add operation → UI: Navigate To Search.
3. Pick the search in Navigate To Search.
4. Public + Visible on Form Menu as needed.

#### Editor sections
1. Shared settings.
2. Navigate To Search.

#### Control reference

| Control | What it does |
|---------|----------------|
| **Navigate To Search** | Dropdown of searches linked on the root unit. Link searches on the data model first. |

#### Pitfalls
- If no linked searches exist, the dropdown is empty.
- Setting a raw number that is not a unit Linked Search binding shows a fake label like `Search 1` in the UI.
- FE-only from the form menu; server composition does not open searches in the browser.


#### Agent metadata
| **actionType** | 53 |

#### Storage mapping
`ActionAttribute.LinkedSearchId` = **unit Linked Search binding id** (`AppTransactionUnitLinkedSearch.Id`), not `AppSearch.SearchId`.

#### Agent hard flow
1. `get_linked_searches` — if empty, continue; if already has rows, pick a `linkedSearchId`.
2. `list_available_searches` — get real `searchId` (+ name). Never invent `1` / `transactionId` / `unitId`.
3. `upsert_linked_search(unitId=rootMaster, searchId, name)` with `confirmed=true` (create ≥1, usually ≥2 when asked). `Action` defaults to **3** (Open Search / ViewSearchResult) — required by server validation.
4. `upsert_command` actionType **53** with `optionsJson.linkedSearchId` = **returned `linkedSearchId`** from step 3.
5. `request_form_reload`.

`upsert_command` rejects orphan `linkedSearchId` values that are not on this transaction’s units.

#### `upsert_command` / `optionsJson` notes
- `actionType`: 53
- `optionsJson.linkedSearchId` = binding id from `get_linked_searches` / `upsert_linked_search` return.

#### Runtime behavior
FE-only: opens linked search configured on root unit.

---

### 3.3 UI: Print

#### Purpose
Print the current form using the client print flow.

#### When to use
User needs a printable view of the saved record.

#### Step-by-step configuration
1. Add operation → UI: Print.
2. Public + menu visibility.
3. Ensure users save before print (unsaved new records may lack data).

#### Editor sections
Shared settings only.

#### Control reference

| Control | What it does |
|---------|----------------|
| *(none beyond Shared settings)* | |

#### Pitfalls
- Requires a persisted root primary key for a meaningful print.


#### Agent metadata
| **actionType** | 48 |

#### Storage mapping
None beyond Shared settings.

#### `upsert_command`
`actionType`: 48

#### Runtime behavior
FE-only print; requires saved root PK.

---

### 3.4 UI: Print From Message Template

#### Purpose
Generate print output using a message template (server-side template render for master-detail).

#### When to use
Formatted printouts driven by template design rather than the default form print.

#### Step-by-step configuration
1. Create or pick a Message Template for this data model.
2. Add operation → UI: Print From Message Template.
3. Select Message Template (chevron opens list; pencil opens editor).
4. Public + menu if user-facing.

#### Editor sections
1. Shared settings.
2. Message Template.

#### Control reference

| Control | What it does |
|---------|----------------|
| **Message Template** | Template picker; chevron = list popup; pencil = template editor. |

#### Pitfalls
- Menu execution posts to server; ensure the template is valid for the transaction.
- Saved record recommended.


#### Agent metadata
| **actionType** | 74 |

#### Storage mapping
Top-level **`AppProjectWorkFlowAction.MessageTemplateId`** (NOT `ActionAttribute`, NOT `notificationMessage`).
UI: `PrintFromMessageTemplateSection` → picker from `retrieveTransactionMessageTemplates(transactionId)`.

#### `upsert_command`
`actionType`: 74; set **`optionsJson.messageTemplateId`** to an existing AppMessage template Id for this transaction.
Do **not** put the template body in `notificationMessage` — that does not bind the Message Template dropdown.

#### Message Template tools (platform-form-config)
1. `upsert_message_template(name?, subject, body)` → `messageTemplateId` (or `get_message_templates` to reuse).
2. `upsert_command(..., optionsJson:{messageTemplateId})` for actionType 74.
3. `request_form_reload` — UI Message Template must show the name, not `(None)`.

#### Runtime behavior
Server invoked from menu; template render path (verify master-detail branch).

---

### 3.5 UI: Save As

#### Purpose
Start a save as / copy flow for the current form in the client.

#### When to use
User duplicates the record into a new form instance.

#### Step-by-step configuration
1. Add operation → UI: Save As.
2. Set name and menu flags.

#### Editor sections
Shared settings only.

#### Control reference

| Control | What it does |
|---------|----------------|
| *(none beyond Shared settings)* | Save-as UI opens at runtime, not in the operation editor. |

#### Pitfalls
- Client menu path; server composition may use different open-form flags (see Execute Data Transfer Need To Open Form).


#### Agent metadata
| **actionType** | 47 |

#### Storage mapping
None beyond Shared settings.

#### Runtime behavior
FE save-as flow.

---

## 4. Backend — Form Refresh, Validate & Save

### 4.1 Save

#### Purpose
Persist form data (same semantic as the form Save action).

#### When to use
Menu Save, or a Composition step before SQL/messaging that needs committed data.

#### Step-by-step configuration
1. Add Save; set Public + Visible on Form Menu for a toolbar/menu save.
2. Or add as Private child in Composition after Validate / formulas.

#### Editor sections
Shared settings only.

#### Control reference

| Control | What it does |
|---------|----------------|
| *(none beyond Shared settings)* | |

#### Pitfalls
- Form menu Save may run on client first; server Composition always persists via server save pipeline.
- Does not run formulas unless a separate Exec DataModel All Formula step exists.


#### Agent metadata
| **actionType** | 49 |

#### Runtime behavior
FE `handleSave` on menu; server `SaveTransactionData` in composition/batch/auto-exec.

---

### 4.2 Refresh

#### Purpose
Reload form data from the server (discard unsaved client changes unless saved).

#### When to use
After external updates, or as last step when child operations changed data.

#### Step-by-step configuration
1. Add Refresh; menu or composition child.

#### Editor sections
Shared settings only.

#### Control reference

| Control | What it does |
|---------|----------------|
| *(none beyond Shared settings)* | |

#### Pitfalls
- Users lose unsaved edits when refreshing.


#### Agent metadata
| **actionType** | 50 |

#### Runtime behavior
FE refresh menu; server reload in composition/batch.

---

### 4.3 Validate Required Fields

#### Purpose
Server-side check that required fields have values; fails the operation if not.

#### When to use
Before Save in Composition (*validate → save → notify*).

#### Step-by-step configuration
1. Add Private Validate Required Fields as first child in Composition.
2. Add Save after it.

#### Editor sections
Shared settings only.

#### Control reference

| Control | What it does |
|---------|----------------|
| *(none beyond Shared settings)* | |

#### Pitfalls
- Not in List Edit picker.
- Does not validate custom business rules beyond required-field metadata.


#### Agent metadata
| **actionType** | 205 |

#### Runtime behavior
`MasterDetailDataFirstLevelValidation` on server; omit from List Edit picker.

---

## 5. Backend — Form Data Processing

### 5.1 Exec DataModel All Formula

#### Purpose
Run all data-model formulas (calculated fields) for the transaction; marks data dirty but does not save.

#### When to use
Before Save when formulas must be up to date, or after DataLoad.

#### Step-by-step configuration
1. Add Exec DataModel All Formula.
2. Optional: click Click To Edit Formulas to maintain formulas on the data model.
3. In Composition: place before Save if calculated fields must persist.

#### Editor sections
1. Shared settings.
2. Exec DataModel All Formula.

#### Control reference

| Control | What it does |
|---------|----------------|
| **Edit Data Model Formulas** / **Click To Edit Formulas** | Opens Application Form Builder on Transaction Formula Setting (shortcut; not stored on the operation). |

#### Pitfalls
- Without a following Save, users see updated values only in memory until they save manually.


#### Agent metadata
| **actionType** | 44 |

#### Runtime behavior
Runs all transaction formulas; dirty state; no save.

---

### 5.2 Execute Command Formula

#### Purpose
Run one assignment formula (command-level expression) against the form.

#### When to use
Targeted calculation not covered by standard field formulas.

#### Step-by-step configuration
1. Add Execute Command Formula (UI section title may show *Execute Commnad Formula*).
2. Enter a non-empty Execute Formula expression using form field tokens from the formula keypad.
3. Typically Private in Composition before Save.

Example: Assign a root status field from other fields, then Save in the same Composition.

#### Editor sections
1. Shared settings.
2. Execute Commnad Formula (UI spelling).

#### How-to notes
- Expression syntax matches the transaction formula family; use the editor’s field helpers when available.
- Prefer this for one targeted assignment; use Exec DataModel All Formula when many field formulas must run.
- Date/time: insert `[Now]` or `[Today]` (same engine tokens as Calculation Flow). Text/memo fields may assign `[Now]` directly (e.g. stamp Notes).

#### Control reference

| Control | What it does |
|---------|----------------|
| **Execute Formula** | Expression editor; must be non-empty. |

#### Pitfalls
- Empty formula fails validation / runtime.
- Putting the expression in `notificationMessage` stores it in the wrong DB column (SQL/message body) — editor will look empty.
- VBA/Excel `Format(Now(), …)` / bare field names will not run.
- Display name retains typo *Commnad* in the UI.


#### Agent metadata
| **actionType** | 57 |

#### Storage mapping
UI: `ActionAttribute.ExecutionFormulaUI` (formulaToken form).  
On save: converted to `ActionAttribute.ExecutionFormula` (internal).  
DB: entire `ActionAttribute` JSON in command `FormulaExpression` column.  
**Never** store the formula in `NotificationMessage`.

#### `upsert_command`
```json
{
  "name": "Stamp Notes",
  "actionType": 57,
  "optionsJson": {
    "linkToUI": false,
    "executionFormulaUI": "[Notes_3499] = [Now]"
  }
}
```
Resolve `[Notes_3499]` via `find_fields("Notes")` → `formulaToken`. Aliases: `executionFormula`, `actionAttribute.executionFormulaUI`.

---

### 5.3 Execute All DataLoad

#### Purpose
Execute all configured DataLoad rules on the data model template.

#### When to use
Populate defaults or lookup-driven values when the form opens or on button click.

#### Step-by-step configuration
1. Configure DataLoad on the data model.
2. Add Execute All DataLoad (often Public + Auto Execute On Form Open).

#### Editor sections
Shared settings only. DataLoad rules live on the data model, not on this operation row.

#### Control reference

| Control | What it does |
|---------|----------------|
| *(none beyond Shared settings)* | |

#### Pitfalls
- Does not save; follow with Save if values must persist immediately.


#### Agent metadata
| **actionType** | 45 |

---

### 5.4 Generate Matrix

#### Purpose
Generate matrix/grid data for a selected matrix unit.

#### When to use
Matrix-style child grids driven by the matrix engine.

#### Step-by-step configuration
1. Define the matrix unit on the data model.
2. Add Generate Matrix and pick Maxtri Unit (UI spelling).
3. Run from menu or composition.

#### Editor sections
1. Shared settings.
2. Generate Matrix.

#### Control reference

| Control | What it does |
|---------|----------------|
| **Maxtri Unit** | Child unit configured as matrix unit on this data model. |

#### Pitfalls
- Wrong unit → empty or incorrect matrix.


#### Agent metadata
| **actionType** | 46 |

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

#### Step-by-step configuration
1. Add Operation (Public or Private tab as appropriate).
2. Set Operation Name and Operation Type → Execute SQL Statement.
3. Configure Execute Condition Field / menu visibility if needed.
4. Open Query Builder with Built-in Tokens.
5. Draft SQL; use Query Design Tool if you need table/column discovery.
6. Insert tokens from Form Root Level Fields, Grid columns, or Built-in Token (do not hand-type field ids unless you must).
7. Click Apply & Close.
8. If SQL returns one value for the form, set Assign Query Scalar Result To Root Field.
9. Save the data model; test from the form Operation menu (avoid Auto Execute On Form Open until verified).
10. For many rows, enable Is Batch Operation (Batch Operation Setting) and confirm tokens/ids match the batch key field.

Example (update current form row):

```sql
UPDATE Crm126_Opportunity
SET Notes = Notes + 'ABC'
WHERE OpportunityId = [TF_####_OpportunityId]
```

Insert `[TF_####_OpportunityId]` by clicking the root primary key field (for example OpportunityId) in the Query Builder—do not invent a placeholder.

Example (scalar into a root field): `SELECT COUNT(*) …` and set Assign Query Scalar Result To Root Field to the target field.

#### Editor sections
1. Shared settings.
2. Collapsible SQL Statement.

#### How-to notes

Token click workflow: Place the cursor in the SQL editor where the value belongs (for example after `WHERE OpportunityId = `), then click OpportunityId under Form Root Level Fields. The token `[TF_####_OpportunityId]` is inserted at the cursor (`####` is the live internal field id).

Current form row: To update/delete only the open record, use the root primary key field button. That inserts the live root-id token — do not type a made-up placeholder such as `[rootid]`.

Apply vs Close: Always finish with Apply & Close. Closing with X discards popup edits; the main editor keeps its previous text.

#### Control reference

Main form (SQL Statement section)

| Control | What it does |
|---------|----------------|
| **SQL Statement** | Monaco SQL editor. Tokens like `[TF_####_FieldName]` are replaced at run time. |
| **Assign Query Scalar Result To Root Field** | Dropdown of root fields. Use when SQL returns one scalar. Choose `(None)` when nothing should be assigned. |
| **Query Builder with Built-in Tokens** | Opens the token/SQL popup beside SQL Statement. |

Query Builder popup

| Control | What it does |
|---------|----------------|
| Left SQL editor | Same SQL as the main field; more space. Cursor receives inserted tokens. |
| **Query Design Tool** | Opens the metadata/query designer. Seeds from current selection if non-empty; otherwise full SQL. Confirming inserts built SQL at the selection. |
| **Apply & Close** | Writes popup SQL back to the operation and closes. |
| **Close** (X) | Closes without applying popup edits. |
| Full screen | Expands the popup frame. |
| **Example: Use Token In Query** | Sample `UPDATE` / `DECLARE` patterns using field tokens and `[CurrentUserId]`. |
| **Global Fields** | (When workflow/globals exist.) Click to insert at cursor. |
| **Form Root Level Fields** | Click a field label to insert its live `[TF_####_…]` token. |
| **Grid {unit} Columns** | Child grid column tokens when batch/child context applies. |
| **Built-in Token** | `[CurrentUserId]`, `[CurrentPartnerId]`, `[CurrentUserName]`, `GETDATE()`, `GETUTCDATE()`. |

#### Pitfalls
- Tokens must match live field ids; renaming fields can invalidate old tokens—re-click fields in the builder.
- Batch SQL runs per source row; verify which record id is in scope.
- Closing the builder with X without Apply & Close drops unsaved popup edits.
- `SELECT` without assign field updates nothing on the form.
- Composition child SQL runs on the server; UI-only types in the same composition do not run SQL for you.


#### Agent metadata
| **actionType** | 42 |

#### Storage mapping
- SQL: column `NotificationMessage` (NOT `ActionAttribute.SqlStatement`).
- Scalar: `ActionAttribute.AssignSqlResultToFiledId`.

#### `upsert_command` / `optionsJson` notes
- Always put SQL in `notificationMessage` when `actionType`=42.
- `assignSqlResultToFiledId` in attributes.
- Tokens: resolve `sqlToken` from `find_fields` / `get_unit_fields` / `rootPrimaryKeys` — format `[TF_{fieldId}_{dbColumn}]`.
- Builtins only: `[CurrentUserId]`, `[CurrentPartnerId]`, `[CurrentUserName]`, `GETDATE()`, `GETUTCDATE()`.
- Good: `UPDATE Crm126_Opportunity SET Notes = Notes + 'ABC' WHERE OpportunityId = [TF_3499_OpportunityId]`
- Bad: no `WHERE`; `WHERE OpportunityId = [rootid]`; inventing tokens without calling field tools.
- Preview may return `warnings` — fix before `confirmed=true`.

#### Runtime behavior
Token replace → execute SQL → optional scalar to root field.

---

### 5.6 Execute Data Transfer

#### Purpose
Run a configured Data Transfer mapping (same family as integration data transfer).

#### When to use
Push/pull data to another data model or external mapping after save or on button.

#### Step-by-step configuration
1. Create Data Transfer Setting.
2. Add Execute Data Transfer; pick the setting.
3. Optionally set target operation and Need To Open Form.

#### Editor sections
1. Shared settings.
2. Execute Data Transfer.

#### Control reference

| Control | What it does |
|---------|----------------|
| **Data Transfer Setting** | Picker for transfer definition; chevron opens list; pencil opens editor. |
| **Execute Task On Target Data Model** | Optional target operation on the destination data model after transfer. |
| **Need To Open Form** | Checkbox — open target form after transfer when appropriate. |

#### Pitfalls
- Available on List Edit models (short picker list).
- Target operation must exist on the destination data model.


#### Agent metadata
| **actionType** | 43 |

#### Storage mapping
`DataTransferSettingId`, `TargetTransactionCommandId`, `IsNeedToOpenNewForm`.

#### Runtime
List Edit allowed type; transfer BL + optional target operation.

---

## 6. Backend — Import & File Generation

Types in this group share collapsible File / Import Settings (paths, import setting pickers, FTP when needed).

### 6.1 Import from Json

#### Purpose
Import JSON file content into database table(s) using a Json import setting.

#### When to use
One known JSON file path per run.

#### Step-by-step configuration
1. Create Json import setting in DB management.
2. Add operation; pick Json Import Setting and File Path.
3. Test with Private composition first.

#### Editor sections
Shared settings + File / Import Settings.

#### Control reference

| Control | What it does |
|---------|----------------|
| **Json Import Setting** | Named import map; use Confirm Selection & Close in the selector popup. |
| **File Path** | Source file; tokens allowed; examples shown in UI (local, http, ftp). |

#### Pitfalls
- Wrong setting → wrong table mapping.
- Path must be reachable from the server.


#### Agent metadata
| **actionType** | 66 |
#### Storage
`JsonImportSettingId`, `FilePath` on attribute + path fields.
#### upsert_command
`jsonImportSettingId`, `filePath`.

---

### 6.2 Import from Excel

#### Purpose
Import one Excel file via Excel import setting.

#### When to use
Single `.xlsx` / similar path per run.

#### Step-by-step configuration
1. Create Excel import setting.
2. Add operation; pick Excel Import Setting and File Path.
3. Test with a small file first.

#### Editor sections
Shared settings + File / Import Settings.

#### Control reference

| Control | What it does |
|---------|----------------|
| **Excel Import Setting** | Import map picker. |
| **File Path** | Excel file path (folder or file URL patterns per UI examples). |

#### Pitfalls
- File lock if Excel has the file open on the server path.


#### Agent metadata
| **actionType** | 67 |
`ExcelImportSettingId`, `FilePath`.

---

### 6.3 Import from Rest-Api setting

#### Purpose
Import using a pre-built Rest-Api Import Setting.

#### When to use
REST pull configured in the integration module.

#### Step-by-step configuration
1. Configure Rest-Api import.
2. Pick Rest-Api Import Setting on the operation.

#### Editor sections
Shared settings + File / Import Settings (setting picker only; no file path).

#### Control reference

| Control | What it does |
|---------|----------------|
| **Rest-Api Import Setting** | Picker + editor shortcuts. |

#### Pitfalls
- Credentials and URL live in the import setting, not on the operation.


#### Agent metadata
| **actionType** | 71 |
`RestApiDbImportSettingId`.

---

### 6.4 Import from DB-To-DB setting

#### Purpose
Import/copy using DB-To-DB Import Setting.

#### When to use
Server-side DB pipeline already defined.

#### Step-by-step configuration
1. Pick DB-To-DB Import Setting.
2. Run from menu or composition.

#### Editor sections
Shared settings + File / Import Settings.

#### Control reference

| Control | What it does |
|---------|----------------|
| **DB-To-DB Import Setting** | Picker. |

#### Pitfalls
- Connection errors surface at runtime in the execution result.


#### Agent metadata
| **actionType** | 72 |
`DbToDbImportSettingId`.

---

### 6.5 Multi Json files in folder

#### Purpose
Import many JSON files under a folder (and subfolders); file names must match import settings.

#### When to use
Batch drop folder processing.

#### Step-by-step configuration
1. Ensure one import setting per file naming pattern.
2. Set File Path to the root folder; test a small folder first.

#### Editor sections
Shared settings + File / Import Settings (File Path = root folder).

#### Control reference

| Control | What it does |
|---------|----------------|
| **File Path** | Root folder (example `c:\MyJsonFileFolder\`). |

#### Pitfalls
- Unmatched file names are skipped or error depending on server rules.


#### Agent metadata
| **actionType** | 76 |
`FilePath` = folder root.

---

### 6.6 Multi Excel files in folder

#### Purpose
Same as Multi Json files in folder for Excel files in a folder tree.

#### When to use
Batch Excel drop folder processing.

#### Step-by-step configuration
1. Set File Path to the folder root.
2. Verify import settings cover each file pattern.

#### Editor sections
Shared settings + File / Import Settings.

#### Control reference

| Control | What it does |
|---------|----------------|
| **File Path** | Root folder for Excel files. |

#### Pitfalls
- Large folders → long run; consider batch operation logging.


#### Agent metadata
| **actionType** | 77 |

---

### 6.7 Download File To Server Folder

#### Purpose
Download/copy a remote or local source file to a server destination path (optional FTP credentials).

#### When to use
Pull attachment or feed file onto app server disk.

#### Step-by-step configuration
1. Set source File Path and Distination File Path (UI spelling).
2. Enter FTP credentials when the path requires them.
3. Run with service account permissions in mind.

#### Editor sections
Shared settings + File / Import Settings.

#### Control reference

| Control | What it does |
|---------|----------------|
| **File Path** | Source (http, ftp, local). |
| **Distination File Path** | Server folder or full target path (UI spelling *Distination*). |
| FTP user/password | Shown when path requires FTP auth. |

#### Pitfalls
- Label spelling *Distination* means destination.
- Overwrite behavior depends on server implementation.


#### Agent metadata
| **actionType** | 73 |
`FilePath`, `DistinationFilePath`, FTP credential fields when used.

---

### 6.8 Convert Xml to Json

#### Purpose
Read XML from File Path, write JSON to Distination File Path.

#### When to use
Integration bridge files.

#### Step-by-step configuration
1. Enter source `.xml` and destination `.json` (or folder) paths.
2. Test with a small sample file.

#### Editor sections
Shared settings + File / Import Settings.

#### Control reference

| Control | What it does |
|---------|----------------|
| **File Path** | Source `.xml`. |
| **Distination File Path** | Target `.json` or folder. |

#### Pitfalls
- Invalid XML fails the step.


#### Agent metadata
| **actionType** | 81 |

---

### 6.9 Convert Json to Xml

#### Purpose
Inverse of Convert Xml to Json — JSON source to XML destination.

#### When to use
Integration bridge files needing XML output.

#### Step-by-step configuration
1. Enter source `.json` and destination `.xml` paths.
2. Test with a small sample file.

#### Editor sections
Shared settings + File / Import Settings.

#### Control reference

| Control | What it does |
|---------|----------------|
| **File Path** | Source `.json`. |
| **Distination File Path** | Target `.xml`. |

#### Pitfalls
- Schema expectations on the downstream consumer.


#### Agent metadata
| **actionType** | 82 |

---

## 7. API, Plugin & External Process

### 7.1 Call API Operation

#### Purpose
Invoke configured API Operation / data transfer mapping with optional logging flags.

#### When to use
Call external REST/SOAP style integration defined in the API Operation library.

#### Step-by-step configuration
1. Define API Operation mapping.
2. Add Call API Operation; pick API Operation.
3. Tune API Call Log Setting checkboxes.
4. Map path/query params in the embedded editor if shown.

#### Editor sections
Shared settings + Call API Operation.

#### Control reference

| Control | What it does |
|---------|----------------|
| **API Call Log Setting** | Checkboxes for what to log (request/response/detail per UI). |
| **API Operation** | Primary mapping picker; opens embedded mapping editor when selected. |

#### Pitfalls
- Wrong mapping id → wrong endpoint.
- Timeouts appear as execution errors.


#### Agent metadata
| **actionType** | 59 |
`DataTransferSettingId` (API mapping), API log flags in section.
Runtime: `CallApiOperationFromMasterDetailTransactionForm`.

---

### 7.2 Call Plugin Web Api

#### Purpose
Call a named method on the plugin Web API bridge.

#### When to use
Custom plugin assemblies expose `WebApiMethodName`.

#### Step-by-step configuration
1. Deploy the plugin on the server.
2. Add Call Plugin Web Api and enter the exact API Method name.

#### Editor sections
Shared settings + Call Plugin Web Api.

#### Control reference

| Control | What it does |
|---------|----------------|
| **API Method** | Plugin method name string. |

#### Pitfalls
- Method name mismatch → runtime error.


#### Agent metadata
| **actionType** | 40 |
`WebApiMethodName`.

---

### 7.3 External Method MasterDetail

#### Purpose
Invoke registered external method (.NET registration) with master-detail context.

#### When to use
Advanced custom code registered in External Method Register.

#### Step-by-step configuration
1. Register the method in the target environment.
2. Add External Method MasterDetail and pick External Method.

#### Editor sections
Shared settings + External Method MasterDetail.

#### Control reference

| Control | What it does |
|---------|----------------|
| **External Method** | Pick from registered methods (loaded from admin API). |

#### Pitfalls
- Assembly must be deployed and registered in the target environment.


#### Agent metadata
| **actionType** | 38 |
`ExternalMethodRegisterId`.

---

### 7.4 Execute External Exe Process

#### Purpose
Start an external executable on the server with arguments.

#### When to use
Legacy EXE tools or command-line utilities (use with security caution).

#### Step-by-step configuration
1. Ensure the exe is allowed on the server.
2. Set File Path and Arguments.
3. Test Private first.

#### Editor sections
Shared settings + File / Import Settings.

#### Control reference

| Control | What it does |
|---------|----------------|
| **File Path** | Full path to `.exe` (example `c:\FolderPath\filename.exe`). |
| **Arguments** | Argument string / JSON per editor. |

#### Pitfalls
- Security: arbitrary exe execution.
- Process may outlive the operation unless the server waits.


#### Agent metadata
| **actionType** | 68 |
`FilePath` (exe), `Arguments`.

---

## 8. Messaging

### 8.1 Shared notification fields

Messaging types (UserId, Email, Partner, SMS) show collapsible Notification Message.

| Control | What it does |
|---------|----------------|
| **Use Rich Text Message Template** | Toggle rich template mode. |
| **Notification Subject** | Subject line (email/partner/user). |
| **Notification Message** | Body text or template body; supports field tokens where the editor offers insert helpers. |
| **Message Template** | Optional template picker when using template mode. |
| **Attach Form Files** | Attach all form files to the message when enabled. |

Configure subject/body here for every messaging type below; destination fields are type-specific.

---

### 8.2 Message UserId

#### Purpose
Send an in-app style message to a user id taken from a form field.

#### When to use
Notify an assigned owner or approver stored on the form.

#### Step-by-step configuration
1. Add Message UserId.
2. Pick Message To UserId Field.
3. Compose subject/body (Shared notification fields).
4. Public menu or Composition child as needed.

#### Editor sections
Shared settings + Notification Message + destination field.

#### Control reference

| Control | What it does |
|---------|----------------|
| **Message To UserId Field** | Any field holding user id. |
| *(plus Shared notification fields)* | Subject, body, template, attachments. |

#### Pitfalls
- Invalid user id → send failure.


#### Agent metadata
| **actionType** | 60 |
`NotificationDestinationPartnerIdTransactionFiledId` (UserId reuse); subject/body in notification fields + `NotificationMessage` column as applicable.

---

### 8.3 Email

#### Purpose
Email to the address in Email Address Field; optional Message Link Operations.

#### When to use
Send email from the form, optionally with approval / action links.

#### Step-by-step configuration
1. Add Email; set Email Address Field.
2. Compose subject/body (Shared notification fields).
3. Optionally add Private children under Message Link Operations (see Message Link Operations).

#### Editor sections
Shared settings + Notification Message + Message Link Operations grid.

#### Control reference

| Control | What it does |
|---------|----------------|
| **Email Address Field** | Root/transaction field with email. |
| **Message Link Operations** | Child operations embedded as links in email (see Message Link Operations). |
| *(plus Shared notification fields)* | Subject, body, template, attachments. |

#### Pitfalls
- Links run later on recipient click, not at send time.


#### Agent metadata
| **actionType** | 61 |
`NotificationDestinationEmailAddressTransactionFiledId`; `ChildActionList` for Message Link Operations.

---

### 8.4 Partner

#### Purpose
Message to a partner from Message To Partner Field; optional Aditional To Email Address CC.

#### When to use
Partner-channel notification with optional extra email recipients and link operations.

#### Step-by-step configuration
1. Pick Message To Partner Field.
2. Optional Aditional To Email Address (UI spelling).
3. Compose notification (Shared notification fields); add Message Link children if needed (Message Link Operations).

#### Editor sections
Shared settings + destination fields + Notification Message + optional Message Link Operations.

#### Control reference

| Control | What it does |
|---------|----------------|
| **Message To Partner Field** | Partner id field. |
| **Aditional To Email Address** | Extra email recipients (UI spelling *Aditional*). |
| **Message Link Operations** | Same as Email (Message Link Operations). |
| *(plus Shared notification fields)* | Subject, body, template, attachments. |

#### Pitfalls
- Partner must resolve to a deliverable channel.


#### Agent metadata
| **actionType** | 62 |
Partner field + `ToEmailAddress`; message links via `ChildActionList`.

---

### 8.5 SMS

#### Purpose
SMS to Phone Number Field; body primarily in Notification Message.

#### When to use
Short text alerts to a mobile number on the form.

#### Step-by-step configuration
1. Select Phone Number Field.
2. Keep the SMS body concise in Notification Message.

#### Editor sections
Shared settings + Phone Number Field + notification fields.

#### Control reference

| Control | What it does |
|---------|----------------|
| **Phone Number Field** | Mobile number field. |
| **Notification Message** | SMS body (keep short). |

#### Pitfalls
- SMS gateway configuration is environment-level.


#### Agent metadata
| **actionType** | 63 |
`SmsMessageToPhoneNumberFiledId`; SMS body in `NotificationMessage`.

---

### 8.6 Message Link Operations

#### Purpose
Define child operations whose links are embedded in Email/Partner messages.

#### When to use
Approval links, “complete task” links.

#### Step-by-step configuration
1. Configure parent Email or Partner.
2. In Message Link Operations, add Private children (for example an Approve Composition).
3. Send the message; the recipient clicks the link to execute the child pipeline.

Example: Email → Message Link child Composition (Validate → Save → status SQL).

#### Editor sections
Under Email or Partner: Message Link Operations grid.

#### How-to notes
- Children should be Private.
- Link execution happens on recipient click, not when the parent message is sent.
- Edit a child via the grid’s row actions (Child Command Editor popup).

#### Control reference

| Column / control | What it does |
|------------------|----------------|
| Add child | Adds Private operations as link targets. |
| **Is Batch Operation** (read-only column) | Shows if child is batch. |
| Grid row actions | Edit child in popup editor. |

#### Pitfalls
- Skip/switch conditions on link children are rarely used until link execution.
- Children should be Private.

#### Agent metadata
Not a standalone `actionType`. Parent Email 61 / Partner 62 store links in `ChildActionList`. See Composition & Message Link children (agent).

#### Storage mapping
Same `ChildActionList[]` shape as Composition children; executed on recipient click (encrypted link), not at send.

#### `upsert_command`
Upsert Private children first, then parent 61/62 with `childActionList` entries. Do not put link SQL on the parent `notificationMessage` unless that is the message body.

---

## 9. Composition

### 9.1 Composition Operation

#### Purpose
Run an ordered list of Child Operations with optional switch field, skip, continue-on-error, and external data model children.

#### When to use
One menu item runs validate → formula → save → SQL → email → close.

#### Step-by-step configuration
1. Set Operation Type → Composition Operation (or pick Composition in the tree).
2. Create Private children (Validate, Save, SQL, …) in the left list first.
3. In Child Operations, add children in order; set Sort.
4. Optional: set Child Operations Switch Condition Field and per-row switch values.
5. Parent Composition is usually Public + menu; children Private.

Example order: Validate Required Fields → Exec DataModel All Formula → Save → Execute SQL Statement → Email → UI: Close Form Window.

#### Editor sections
Shared settings (Category hidden) + Composition Operation.

#### How-to notes
- Prefer creating Private children in the left list, then attaching them to the grid—empty shells with no children do nothing useful.
- Add can attach an existing Private operation, create a new internal child, or add an external data model child.
- UI Close/Navigate as children behave differently on pure server workflow vs client menu (see pitfalls).

#### Control reference

| Control | What it does |
|---------|----------------|
| **Child Operations Switch Condition Field** | Root dropdown/list field; enables per-child Child Task Switch Condition values in the grid. |
| **Child Operations** grid | Order (Sort), operation name, skip flags, continue on error, batch flag (read-only), switch condition, change-trigger fields, external data model, etc. |
| **Add** dropdown | Existing Private operation, new internal child, or external data model child. |

Popups: Available Child Operations selector; Child Command Editor; external Application Form Builder when editing an external model child.

#### Pitfalls
- UI Close/Navigate children do not run on pure server workflow the same as client menu.
- Go To Next With Error changes failure handling—use deliberately.
- External children require permissions on other data models.


#### Agent metadata
| **actionType** | 200 |

#### Storage mapping
`ActionAttribute.ChildActionList[]`, `ChildCommandsSwitchConditionFieldId`.

#### `upsert_command`
- `actionType`: 200
- Required workflow (same as UI):
  1. Create each child as its own Private operation (`linkToUI: false`) — e.g. SQL child `actionType` 42 + `notificationMessage`.
  2. `get_commands` / note each child’s `commandId`.
  3. Upsert the Composition with `optionsJson`:
     `{"linkToUI":true,"isShowOnTopMenu":true,"childActionList":[{"commandId":123,"sort":1},{"commandId":456,"sort":2}]}`
  4. `request_form_reload` — Child Operations grid binds `ActionAttribute.ChildActionList`.
- Wrong: create only a 200 shell (or put SQL on the composition’s `notificationMessage`) → UI Child list stays empty.
- Children normally `linkToUI: false`. Parent usually Public + menu.

---

## 10. Scenarios (I want to…)

| I want to… | Configure |
|------------|-----------|
| Menu button that saves | Save, Public, Visible on Form Menu |
| Validate then save then email | Composition (Public) with Private children: Validate Required Fields → Save → Email |
| SQL using OpportunityId in WHERE | Execute SQL Statement → Query Builder → click OpportunityId → Apply & Close |
| Put SQL scalar into a field | Assign Query Scalar Result To Root Field + `SELECT` one column |
| Run only if user checked Approved | Execute Condition Field = Approved checkbox |
| Update every selected search row | Is Batch Operation, Source Search, key view field, then SQL or API |
| Close form after success | Last step: UI: Close Form Window (client menu/composition) |
| Email with approval link | Email operation + Message Link Operations child Composition |
| On form open: load defaults | Public Execute All DataLoad + Auto Execute On Form Open (only one auto-open recommended) |
| Import nightly JSON drop | Private Multi Json files in folder in scheduled workflow composition |

---

## 11. Good practices

- One Public operation per user intent; keep helpers Private in Composition.
- Name operations for end users, not internal codes.
- After changing Operation Type, review all collapsible sections for stale values.
- Test from the real form menu before Auto Execute On Form Open.
- Prefer Query Builder token clicks over hand-typed `[TF_…]` tokens.
- Order Composition children: validate → calculate → save → integrations → notify → UI close.
- Use Execute Result Console Display Type = details while developing; shorten for production if needed.
- Document designer intent in Notes.

---

---

## 12. Composition & Message Link children (agent)

`ChildActionList[]`: `Sort`, `CommandId`, `CommandDisplay`, `ExternalTransactionId`, `IsSkip`, `IsGoToNextCommandWithError`, `PredictValue`, `ChangeTriggerRootLevelFieldId`, `ChangeTriggerChildGridUnitId`.

Message link children: types 61, 62; links encrypted at send; executed when recipient clicks (not at send).

## 13. Execution pipeline

1. Form menu: FE-only types 47, 48, 49, 50, 53, 54 → client; others POST execute.
2. Server `ExecuteTransactionRootCommand`: condition → batch loop → composition or single executor.
3. Auto on open: first `LinkToUI && IsAutoExecuteOnFormOpen`.
4. List Edit server: strong support for 43, 49, 50.

## 14. `upsert_command` checklist

1. Read this KB + `get_command_detail` shape.
2. Correct actionType (`EmAppTransactionCommandType`), never menu link targets 1/2.
3. SQL (42): `notificationMessage` column.
4. SMS (63) / notifications: often `notificationMessage` for body.
5. Public flags: `optionsJson` → `linkToUI`, `isShowOnTopMenu`, `isAutoExecuteOnFormOpen`, `actionFlowOrder`.
6. Preview → `ask_user` → `confirmed=true` → `request_form_reload`.

## 15. Scenarios (I want to…) — agent

| Goal | actionType / pattern |
|------|----------------------|
| Menu save | 49 + `linkToUI` + `isShowOnTopMenu` |
| Validate → save → email | 200 composition children 205, 49, 61 (private) |
| SQL on OpportunityId | 42 + `notificationMessage` with `[TF_*_OpportunityId]` token |
| Batch search update | parent `isBatchCommand` + child 42 or 59 |

## 16. Good practices (agent)

- Mirror `get_command_detail` property casing in JSON patches.
- Never run operations from Configurator agent.
- After `upsert_command`, rely on `request_form_reload` for UI refresh.

## Change log

| Date | Note |
|------|------|
| 2026-11-17 | Replaced § cross-references with plain section names (mirrors User Help). |
| 2026-11-17 | Restructured to mirror User Help chapter order (step-by-step before controls); retained agent metadata/storage/upsert; renumbered agent-only chapters 12–16. |
| 2026-11-17 | Full UI depth (mirrors User Help) + per-type actionType/storage/upsert. |
