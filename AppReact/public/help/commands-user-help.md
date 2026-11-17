# Data Model Operation — Help

**Audience:** Form designers using Application Form Builder / Workflow Automation.  
**Related agent KB:** [AppConfigurator-Commands-AgentKnowledge.md](./AppConfigurator-Commands-AgentKnowledge.md) (technical; not for end users).

A **Data Model Operation** (short: **Operation**) is an action you attach to a data model: save, refresh, run SQL, send email, call an API, run a sequence, and so on. Users can run **public** operations from the form menu; **private** ones are for sequences and internal steps.

---

## 1. Concepts

| Term | Meaning |
|------|---------|
| **Operation** | One named action on this data model (Save, Refresh, Execute SQL, …). |
| **Operation Type** | What the operation does. Choose it from the tree picker (Category is shown under the type for guidance only). |
| **Public** | Can appear on the form menu, calendar/Gantt menus, or run automatically when the form opens (depending on checkboxes). |
| **Private** | Hidden from the form menu by default. Use for steps inside a Composition or message links. |
| **Composition** | One operation that runs several child operations in order. |
| **Batch** | Run the same operation once per row (from a dataset, search result, or child grid). |

---

## 2. Finding the right Operation Type

Use the **Operation Type** tree. Groups:

| Group | Use when you want to… |
|-------|------------------------|
| **UI Operations** | Close the window, open a linked search, print, save-as. |
| **Backend → Form Refresh, Validate & Save** | Save data, reload the form, or check required fields. |
| **Backend → Form Data Processing** | Run formulas, data loads, matrix generation, SQL, or data transfer. |
| **Backend → Import & File Generation** | Import Json/Excel/Rest/DB, multi-file import, download, Xml↔Json. |
| **API, Plugin & External Process** | Call an API mapping, plugin web API, registered external method, or external program. |
| **Messaging** | Send email / SMS / partner / user-id based messages from form fields. |
| **Composition** | Chain several operations in a fixed order. |

**List Edit** data models offer fewer types (mainly Save, Refresh, Data Transfer, Composition).  
**Workflow Automation** does not offer window-only UI types (close / print / navigate / save-as).

---

## 3. Shared settings (every operation)

### Basic options

| Setting | What it does |
|---------|----------------|
| **Operation Name** | Label on the form menu and in lists. |
| **Operation Type** | Behavior and which extra sections appear below. |
| **Category** | Read-only path from the type picker (not saved). Hidden for Composition. |
| **Flow Order** | Order in the list and on the form menu. |
| **Execute Condition Field** | Optional checkbox field: operation runs only when that field is checked. |
| **Is Public Operation** | Allows menu / auto-open options. |
| **Visible on Form Menu** | Show in the form’s operation menu (needs Public). |
| **Visible on Calendar/Gantt/Scheduler Menu** | Show on those view menus. |
| **Auto Execute On Form Open** | Run once when the form opens (needs Public). |

Tips:
- New operations follow the **Public** / **Private** tab you are on.
- Switching Operation Type does not clear old type-specific fields—review the sections below after a change.
- Turning off Public does not clear the three visibility / auto-open flags; uncheck those if you no longer need them.

### Status log

Useful mainly for **Workflow Automation** roots: whether to log start/end and error details. For ordinary form operations you can leave logging off.

### Execution result display

Control how success/failure messages appear after the operation runs (detail vs status only, optional custom success text).

### Batch

Turn on **Is Batch Operation** when the same step should run for each row from a dataset, a search, or a child unit. Then pick the source and key field/unit.

### Notes

Free-text documentation for yourself or other designers (not shown to end users).

---

## 4. Composition and message links

### Composition

1. Set Operation Type to **Composition** (or pick the Composition group in the tree).  
2. Add **child operations** in order.  
3. Children are usually **Private**.  
4. Optional: skip a child, continue after error, or branch using a switch field / predict value.

Typical pattern: calculate → save → send message.

### Message link operations

For **email** and **partner** message types you can attach child operations that become links inside the sent message. The recipient runs them later by opening the link—not when the message is sent.

---

## 5. Quick “I want to…” guide

| Goal | Suggested type |
|------|----------------|
| Save the form from a menu button | Save (Public + Visible on Form Menu) |
| Reload after external changes | Refresh |
| Block save when required fields empty | Validate Required Fields (often inside Composition before Save) |
| Run SQL against the database | Execute SQL Statement |
| Copy/map data with a transfer setting | Execute Data Transfer |
| Call a configured API | Call API Operation |
| Email the customer from a form field | Messaging → Email (set destination + body/template) |
| One menu item that does several steps | Composition with private children |
| Import an Excel/Json file | Import types under Import & File Generation |
| Close the form after success | UI: Close Form Window (often last step of a Composition for the user path) |

---

## 6. Good practices

- Prefer **one clear Public** operation per user action; keep supporting steps **Private** inside Composition.  
- Name operations for the user (“Submit Opportunity”), not for internals (“SQL1”).  
- Test Public menu visibility and execute condition on a real form.  
- For Workflow Automation, design the tree of operations there; the Form Builder list is not the primary editor for workflow roots.

---

## Change log

| Date | Note |
|------|------|
| 2026-11-17 | User-facing help split from agent KB (no ActionType ids / storage internals). |
