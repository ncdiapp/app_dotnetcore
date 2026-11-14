# AppBuilder Glossary (plain language)

Status: DRAFT v0.1. Terms below were taken from the React UI source (`AppReact/src/components/admin/MyApplicationEditor/*`) and `Document/AgentDesign/MyApplicationEditor-AiAgent-Design.md`. Items marked **(verify in UI)** have not been checked in a running app and need a human review before the tutor uses them.

Rule for the tutor: use the **screen label** the learner sees, and explain it in one sentence the first time.

| Screen label / term | Plain-language meaning | Notes |
|---|---|---|
| **Application** | One complete app (for example "Leave Tracker") that holds all its data models, lists, workflows and menus. | A learner opens it through **My Application Editor**; the tab is titled `Config: {ApplicationName}`. |
| **My Application Editor** | The place where you set up and change an application (not where end users do their daily work). | Left menu has sections such as Application Properties, Data Model Design, Entity List Of Value, Report & View, Import Config, Database, Workflow, Menu Setting. **(verify in UI)** the exact order. |
| **Data Model Design** | The section where you define what information the app stores, for example a leave request and its fields. | The grid lists each data model with Name, Modified Date, Default Search, Folder Tree, Created From, Data Source From, Description. |
| **Data Model** (also called **Transaction** in code and some screens) | One kind of record the app keeps, such as "Leave Request", with its fields and how it is laid out. | The code calls it a *Transaction*. Teach "data model" first, then mention that some screens say "Transaction". |
| **Form Model** | A data model that is shown as a form for entering one record at a time (can have child rows under it, "master-detail"). | Shown as "1. Form Model" in the grid. |
| **List Edit Model** | A data model that is edited as a table of rows. | Shown as "2. List Edit Model". |
| **Excel Import Draft** | A data model created from an Excel file. | Shown as "3. Excel Import Draft". The Excel create option shows "not yet implemented" in the current React UI. |
| **Data Source** | Where the data is stored (a registered database). | The Create menu lets you pick one when more than one exists. |
| **Default Search** | The list/search screen that opens by default for a data model. | Shown per row in Data Model Design. |
| **Folder Tree** | A tree-style navigation for browsing records. | Optional. Not used in Leave Tracker. |
| **Entity List Of Value** | A reusable list of choices, such as the values of a dropdown. | Careful: here "Entity" means a list of choices, **not** a data model. Needed for the LeaveType dropdown. **(verify in UI)** |
| **Report & View** | Where saved searches, lists and views are built. | Used for My Requests and Pending Approval. **(verify in UI)** |
| **Import Config** | Load a prepared set of settings (a configuration pack) into the application. | Basis for lesson reset and skip-ahead. |
| **Workflow** | The steps a record goes through, for example Submitted -> Approved. | Lesson 6. **(verify in UI)** |
| **Menu Setting** | Which items appear in the app's menu for end users. | **(verify in UI)** |
| **Assistant** | The AI chat that can create and change application settings for you. | **Planned**: the design says an "Assistant" dock opens inside My Application Editor, but it is not in `MyApplicationEditor.tsx` yet. Today the same agent is reached via the App Builder Agent page. |
| **Configurator** | The AI chat on a running form or search that adjusts its layout, formulas and columns. | Narrow by design: it cannot create new things from scratch (per the design doc). |

## Terms to avoid or explain first
- *Schema, table, DTO, API, entity* (the developer sense). Say "list of fields" or "the data you store".
- *Transaction* on its own. Say "data model (Transaction)".
- *HITL / config pack / SaaS application id*: internal. Say "the assistant will ask you to confirm" and "a saved set of settings".
