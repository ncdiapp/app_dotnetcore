# Transaction Form Agent — In-Chat App Configuration & Design

**Status:** Design draft — awaiting requirement lock  
**Date:** 2026-11-09  
**Parent:** [TransactionForm-AiAgent-Plan.md](./TransactionForm-AiAgent-Plan.md)  
**Related:** [Agent-Integration-Playbook.md](./Agent-Integration-Playbook.md) (AppConfigPack), V028 `platform-transaction` / `platform-search`, V070 Form Agent seed

---

## 1. Why

Form Agent today operates **business data** on the open Master Detail / List Edit form (`platform-form-host`: `get_form_state`, `set_fields`, `update_grid`, linked search popup, commands).

Parent plan §9 explicitly outscoped:

> Letting the agent edit form design (fields, layout, or linked-search configuration).  
> Subscribing `platform-transaction` or `platform-search`.

Users now want to stay in the form chat and change **App Configuration** for the current transaction: field control types, labels, entity DDL wiring, linked searches, form/layout options, and related Search / Entity config — without opening Transaction Graphic Editor / Form Design UI.

This document is the sub-plan for that capability.

---

## 2. Goal / Non-goals

### Goal

From the Form Agent dock on an open transaction form, the user can ask the agent to:

1. **Inspect** the current transaction / form / unit / field / linked-search / entity configuration.
2. **Change** configuration of the **currently open** transaction (and closely related Search / Entity objects the form already depends on).
3. **See the runtime form reflect** those config changes without a full app restart (reload metadata while preserving unsaved FormData when possible).

### Non-goals (v1)

- Building a brand-new application / hierarchy from the form dock (`create_application`, `create_hierarchy_from_tables`, `propose_schema` / `execute_approved_schema`).
- Deleting the open transaction (`delete_transaction`).
- Creating unrelated Search screens or menu entries across the tenant.
- Full pixel-perfect Form Design drag-layout parity (arbitrary multi-row reshuffle) in v1.
- Replacing Transaction Graphic Editor; chat is an accelerator for common edits.
- Listing Form Agent in the left menu (unchanged).

---

## 3. Current state (what already exists)

| Layer | What we have | Gap for Form Agent |
|---|---|---|
| Form data tools | `platform-form-host` + `FormHostWriteBL` Calculation-style apply | Config writes are not host_tool; they persist to DB |
| Transaction mutate | `TransactionModifierPlugin`: `update_transaction_field`, `set_field_entity`, `delete_transaction` | Only `displayName` / `controlType` / `entityId` / `defaultValue`; no Required/ReadOnly/layout/linked search; no current-TX scope lock |
| Transaction read / create | `get_transaction_details`, `get_existing_transactions`, create_* tools | Create_* are too broad for form dock; details payload may be heavy |
| Entity | `list_entity_data_sources`, `create_entity_simple_list`, `create_entity_from_table` | Need “attach existing entity” vs “create new entity” policy |
| Search | `list_*`, `get_search_criteria`, `create_search`, `create_search_view` | No “edit existing search criteria / column set” tool; no “wire linked search on unit” tool |
| Form layout | `AppFormFlexLayoutBL` + AppConfigPack form-layout apply | **No** agent BuiltIn tools for layout item / visibility / column span |
| Pack path | `platform-app-config-pack` validate / preview / execute | Powerful but heavy for “make Status a dropdown”; better as Phase 3 escape hatch |
| Host UI | Catalog `HostContextJson` each turn; FormData bind after data tools | **No** “reload transaction metadata / form shell” after config save while record stays open |
| Prompt (V070) | “Do not change form design…” | Must be rewritten when config mode ships |

---

## 4. Recommended approach

**Do not** wholesale subscribe Form Agent to full `platform-transaction` + `platform-search`.

Those libraries mix **read**, **safe mutate**, and **destructive / greenfield create** tools. Form dock context needs a **scoped config surface** with:

1. Hard **current-transaction binding** (HostContext `transactionId`).
2. Explicit **allow / deny** tool lists.
3. A **host reload** step after successful config writes.
4. **HITL** (`ask_user`) before durable config writes (and always before create/delete).

### Preferred shape: new library `platform-form-config`

| Option | Description | Verdict |
|---|---|---|
| **A. New library `platform-form-config`** | Thin BuiltIn tools that wrap existing BL (`AppTransactionBL`, `AppFormFlexLayoutBL`, LinkedSearch BL, Entity BL). Methods take optional ids but **server forces** `transactionId` from HostContext / session. | **Recommended** |
| B. Subscribe `platform-transaction` + exclusions | Faster seed; agent can still pass another `transactionId`; create/delete exclusions are fragile; tool descriptions still talk about App Builder flows | Acceptable interim only if A is delayed |
| C. AppConfigPack-only | Agent drafts pack JSON for one TX and executes | Too heavy / error-prone for iterative field tweaks; keep as Phase 3 |

**Composition:**

```text
Form Agent subscriptions (additive)
  platform-form-host          — runtime FormData (unchanged)
  platform-form-config        — NEW: scoped config inspect + mutate
  platform-database           — keep existing DDL exclusions
  platform-api-provider / data-analyze / platform-ui-render — unchanged

Optional later
  platform-app-config-pack    — bulk / complex layout via pack (Phase 3)
  selected read-only tools from platform-search (list / get criteria)
```

Reuse plugin *code* from `TransactionModifierPlugin` / Entity plugins where possible; prefer new methods that:

- Resolve `transactionId` from `AgentRunContext.HostContextJson` when omitted.
- Reject when caller’s `transactionId` ≠ host open TX (unless admin flag later).
- Return a standard `{ ok, changed, reloadRequired: true, summary }` envelope so the host can reload.

---

## 5. Capability phases

### Phase 1 — Inspect + field property edits (MVP)

User stories:

- “What control type is Status? Is it bound to an entity?”
- “Rename label Customer → Sold To.”
- “Make Status a dropdown bound to entity OrderStatus.”
- “Set default value / make field required / read-only.”

Tools (illustrative names):

| Tool | Persist? | Notes |
|---|---|---|
| `describe_form_config` | No | Compact config for current TX: units, fieldId, db, label, controlType, entityId, required, readOnly, layout hints, linked searches summary |
| `get_field_config` | No | One field deep dive |
| `update_field_config` | Yes | Extends today’s `update_transaction_field` keys: `displayName`, `controlType`, `entityId`, `defaultValue`, `isRequired`, `isReadOnly`, `isVisible` (and layout label sync via existing `SaveAppTransactionFieldExDto` path) |
| `set_field_entity` | Yes | Reuse modifier logic; scoped to current TX |
| `list_entity_data_sources` | No | Reuse |
| `reload_form_host` | Host | After config write: host refreshes metadata; see §7 |

HITL: `ask_user` before first durable write in a turn (or every write — product choice, see Open Questions).

Seed: subscribe `platform-form-config`; update Form Agent system prompt; **do not** remove form-data tools.

### Phase 2 — Linked Search / Link Target / Transaction options

User stories:

- “Add a Linked Search on Order Line to Product Search; map SKU ← ProductCode.”
- “Remove this linked search.”
- “Turn on AI agent dock by default for this transaction.” (edit `TransactionOptionDto` / OtherOptions)

Tools:

| Tool | Persist? |
|---|---|
| `list_linked_searches` | No |
| `upsert_linked_search` | Yes — searchId + field mappings |
| `delete_linked_search` | Yes — HITL |
| `list_link_targets` / `upsert_link_target` | Yes |
| `update_transaction_options` | Yes — allowlisted option keys only |

Depends on existing LinkedSearch / LinkTarget BL save APIs (already used by Graphic Editor). May need thin wrappers; not present as agent tools today.

### Phase 3 — Form layout (Design) + Search / Entity create (guarded)

User stories:

- “Hide Remark from the form.” / “Move Qty next to SKU.” / “Use 3-column layout for Header.”
- “Create a simple entity list for Priority and bind the field.”
- “Create a Product lookup search and attach it as Linked Search.”

Tools / paths:

| Capability | Approach |
|---|---|
| Layout visibility / span / order (limited) | New tools over `AppFormFlexLayoutBL` for **item-level** updates; avoid full layout JSON in LLM args |
| Rebuild default layout | Optional `rebuild_default_form_layout(columns)` with strong HITL |
| Complex layout | Draft AppConfigPack form-layout section → validate → ask_user → execute |
| Create entity / search | Allow **only** when result is immediately wired to current TX; still HITL; never menu publish unless asked |

Full drag-canvas parity stays out of scope; Graphic Editor remains the authority for complex design.

### Explicitly excluded forever (or until a separate App Builder dock agent)

- `delete_transaction` on the open form
- `propose_schema` / `execute_approved_schema` / `alter_table` / `create_database_table` / `confirm_drop_tables`
- `create_application` / `create_hierarchy_from_tables` / `delete_application`
- Cross-tenant or arbitrary other-transaction mutation without leaving the form host

---

## 6. Dual mode: Data vs Config

Same Specialist (`app-transaction-form-agent`), same dock, **two tool families**. Prompt must teach the distinction:

| Intent | Tools | Persistence |
|---|---|---|
| Fill / change record values | `set_fields`, `update_grid`, … | Browser FormData only until user Save |
| Change form definition | `update_field_config`, linked search, layout, … | **Immediate DB write** after HITL |

Rules:

1. Never use config tools to “set the Status of this order” — that is FormData.
2. Never use `set_fields` to “make Status a dropdown” — that is config.
3. After config success, call host reload before further FormData tools that depend on ControlType / EntityId / catalog.
4. If FormData is dirty, reload policy must preserve values (§7).

Optional UX (Phase 1.5): dock sub-tab or chip “Config mode” that only changes prompt emphasis / tool preference — not a second agent.

---

## 7. Host reload after config write (critical)

Config tools write server-side metadata. The open React form still holds:

- Cached `AppTransactionExDto` / form layout used for rendering controls
- Catalog used for `HostContextJson`
- In-memory `currentFormData` (possibly dirty)

Without reload, the user sees old ControlTypes / missing Linked Searches.

### Proposed contract

1. Config tool returns `{ ok: true, reloadRequired: true, transactionId, changed: [...] }`.
2. Engine / host bridge: if `reloadRequired`, emit a **host signal** (prefer dedicated `host_tool` `reload_form_metadata`, or a structured flag on tool result that `AgentUiChatHost` / FormSidePanel already watches).
3. Host action:
   - Refresh hierarchy + form layout from server (same APIs Graphic Editor / form open use).
   - Rebuild control tree.
   - Rebind previous FormData by PK / row keys; coerce where ControlType changed (best effort; report fields that could not coerce).
   - Rebuild next-turn `HostContextJson` catalog (include richer config for agent if Phase 1 `describe_form_config` is used instead of stuffing HostContext).
4. If dirty and coercion is lossy → `ask_user`: “Reload will keep your unsaved edits where possible. Continue?”

Do **not** auto-Save the business record as part of config reload.

---

## 8. Security & authorization

| Concern | Policy |
|---|---|
| Who may use config tools | Same users who may open Transaction / Form design for that TX (reuse existing permission checks inside BL save methods; fail closed with clear error) |
| Scope | Server binds to HostContext `transactionId`; reject mismatches |
| HITL | Durable config writes require `ask_user` confirmation summarizing the change |
| Audit | Log agentId, userId, transactionId, tool, args summary, success (align with future AgentSecurity audit; minimum: NLog + existing BL trails) |
| DDL | Keep current `platform-database` exclusions on Form Agent |
| General Agent | Do **not** add `platform-form-config` to General Agent allowlist by default |

Safe Mode plan remains separate; Form Config tools are BuiltIn allowlisted methods, not script execution.

---

## 9. Context & prompt changes

### HostContextJson

Keep catalog **value-free**. Optionally add light config hints so the model does not call `describe_form_config` every turn:

```json
"configHints": {
  "formId": 88,
  "fieldCount": 42,
  "linkedSearchCount": 3,
  "canEditConfig": true
}
```

Full fieldId / controlType / entityId belong in `describe_form_config` / `get_field_config`, not in every-turn HostContext (token cost).

### System prompt (replace V070 “Do not change form design”)

Sketch:

- You may change **configuration** of the **open** transaction via `platform-form-config` after `ask_user`.
- Prefer `describe_form_config` before mutate.
- After a successful config write, reload the form host before editing FormData that depends on the change.
- Do not create applications, drop tables, or delete this transaction.
- Distinguish FormData edits vs config edits.

Upgrade script: new migration updates prompt only when still equal to the V070 stock text (same IF-NOT-customized pattern as other agent seeds), or document that admins who customized prompts must merge manually.

---

## 10. Work breakdown (implementation checklist)

### Design lock

- [ ] Confirm phases / MVP scope (§5, Open Questions)
- [ ] Confirm HITL strictness and dirty-reload behavior
- [ ] Confirm library strategy (A vs B)

### Backend

- [ ] New plugin e.g. `FormConfigPlugin` (or extend modifier with scope guards)
- [ ] Seed library `platform-form-config` + tools + Form Agent subscription + exclusions
- [ ] Expand field update keys; wire Required / ReadOnly / Visible through `SaveAppTransactionFieldExDto`
- [ ] Phase 2: LinkedSearch / LinkTarget / OtherOptions wrappers
- [ ] Phase 3: layout item tools and/or AppConfigPack path
- [ ] Enforce HostContext transactionId on all mutate tools
- [ ] Permission checks + structured `{ reloadRequired }` results
- [ ] Prompt migration for `app-transaction-form-agent`

### Frontend (Form Agent host)

- [ ] Handle `reload_form_metadata` (or equivalent) host_tool
- [ ] Preserve dirty FormData across metadata reload
- [ ] Refresh catalog / actions list after reload
- [ ] Surface config-change confirmation UX if needed beyond generic `ask_user`

### Docs / parent plan

- [ ] Parent plan §4 / §9: mark config design as this sub-doc; remove absolute “never subscribe” once Phase 1 ships
- [ ] Agent Management help text: Form Agent now includes scoped config tools

### Test scenarios

- [ ] Rename label → reload → catalog shows new label; FormData unchanged
- [ ] Text → DDL + entity → control renders dropdown; existing Id value still selected
- [ ] Required flag → Save validation behaves correctly
- [ ] Linked Search upsert → `open_linked_search` sees new action
- [ ] Reject config tool aimed at another `transactionId`
- [ ] Dirty FormData + ControlType change → user warned; values preserved or listed as dropped
- [ ] Agent Management test chat without form host → config mutate returns clear error

---

## 11. Open questions (need product lock)

1. **MVP scope:** Phase 1 only first, or Phase 1 + Linked Search (Phase 2) in the same release?
2. **HITL:** Confirm every config write, or confirm once per “batch” after the agent proposes a plan?
3. **Who can edit config from the form:** any user with form open, or only users with Transaction Design / Admin rights?
4. **Create Entity / Search from form chat:** allow in Phase 3, or forever push to App Builder agent?
5. **Layout:** item-level tools in Phase 3, or skip straight to AppConfigPack for layout?
6. **Dirty form policy:** always attempt preserve + warn, or require user Save / Discard before config reload?

**Recommendation:** ship Phase 1 (inspect + field config + host reload) first; Phase 2 Linked Search next; defer layout / create Entity-Search to Phase 3 after reload UX is solid.

---

## 12. Parent plan deltas (when approved)

When this design is locked, update [TransactionForm-AiAgent-Plan.md](./TransactionForm-AiAgent-Plan.md):

- §4 Tools: add `platform-form-config`; note selective reuse of transaction/search capabilities.
- §9 Out of scope: remove “edit form design…” for items covered by locked phases; keep greenfield create / delete / DDL out of scope.
- §10 Implementation order: append Form Config phases after FormData host tools are stable.

---

## 13. Decision log

| Date | Decision |
|---|---|
| 2026-11-05 | Parent plan: no form design; no `platform-transaction` / `platform-search` on Form Agent |
| 2026-11-09 | Sub-design opened: scoped in-chat config via recommended `platform-form-config` + host reload |
