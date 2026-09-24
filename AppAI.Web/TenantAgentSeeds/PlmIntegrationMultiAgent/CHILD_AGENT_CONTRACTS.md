# PLM Multi-Agent — Child SkillKey / call_agent contracts

**Status:** Wave 0+1 live. Search / MassUpdate children are Wave 2 (ROOT still runs those tools).  
**ROOT:** `plm-integration-orchestrator` (Interactive, all HITL, `IsActive=1` — only agent on the left menu).  
**Children:** Deterministic, `IsActive=0` (hidden from left menu). `call_agent` loads by SkillKey. Never `ask_user` / PlanGate wait.  
**Missing SkillKey:** `call_agent` errors; ROOT stops (Retry / Back). No local preview/execute fallback.

## Platform rules (non-negotiable)

| Rule | Detail |
|---|---|
| HITL only on ROOT | Child must not call `ask_user`. Missing input → structured error JSON for ROOT. |
| call_agent is headless | New empty history each call; pass phase + ids in message; durable facts in shared context / session. |
| Shared context | Same WorkflowId as ROOT for the live turn. After restart, `get_plm_wizard_progress` reads `AppAgentSharedContext` ScopeId=`ChatSessionKey`. |
| Libraries | Child subscribes only to tools it needs (`integration-plm-import`, `agent-files`, …). ROOT keeps `platform-multi-agent`. |
| Return shape | Short status JSON in FinalResponse (see below). No huge SQL/JSON dumps. |

### Standard child FinalResponse (JSON text)

```json
{
  "ok": true,
  "skillKey": "plm-integration-entity",
  "phase": "execute",
  "sessionId": 123,
  "summary": "Entity import completed",
  "counts": { "inserted": 0, "updated": 0, "skipped": 0 },
  "jobId": null,
  "errors": [],
  "nextHint": "Mark wizard.entity=done"
}
```

On failure: `ok=false`, fill `errors[]`, leave wizard step unchanged (ROOT decides Retry).

---

## SkillKey catalog

| SkillKey | Wizard code | Seeded? | Kind |
|---|---|---|---|
| `plm-integration-import-dw` | `import-dw` | **Yes** (`02_*.sql`) | Repeatable by TemplateId |
| `plm-integration-entity` | `entity` | **Yes** (`04_*.sql`) | Linear once |
| `plm-integration-folder` | `folder` | **Yes** (`05_*.sql`) | Linear skippable (+ PHASE=PLACEMENT) |
| `plm-integration-image` | `image` | **Yes** (`06_*.sql`) | Linear skippable |
| `plm-integration-color` | `color` | **Yes** (`07_*.sql`) | Linear skippable |
| `plm-integration-pom` | `pom` | **Yes** (`08_*.sql`) | Linear skippable |
| `plm-integration-search` | `search` | Later | Repeatable (main Search **or** additional View on an existing Search) |
| `plm-integration-massupdate` | `massupdate` | Later | Repeatable |

**No `plm-integration-sibling`.** Additional Search View (former Sibling View / Card attach) is **the same wizard step and the same child** as Import Search View. MassUpdate stays separate.

`connect` / `techpack-schema` stay on ROOT (Gate-0 + `ensure_techpack_schema`).

---

## Shared context keys

### Always present (ROOT owns)

- `plm.integration.job` — Gate-0 ids + `sessionId`
- `plm.integration.wizard` — checklist (also durable via `update_plm_wizard_progress`)

### Per-child envelopes

Pattern: `plm.integration.{code}.inputs` | `.plan` (if HITL mid-flight) | `.outputs`

| Child | Keys |
|---|---|
| import-dw | `…import-dw.inputs`, `.phase-a`, `.plan`, `.outputs` |
| entity | `…entity.inputs`, `.outputs` |
| folder | `…folder.inputs`, `.outputs` (include `runPlacement: bool`) |
| image | `…image.inputs`, `.outputs` |
| color | `…color.inputs`, `.outputs` |
| pom | `…pom.inputs`, `.outputs` |

`inputs` minimum:

```json
{
  "sessionId": 123,
  "saasApplicationId": 1,
  "plmDataSourceId": 12,
  "dwDataSourceId": 13
}
```

Children read `sessionId` from inputs or `plm.integration.job`; never invent connection strings.

---

## call_agent message contracts

ROOT writes inputs (and plan when needed), then:

```text
call_agent("<SkillKey>", "<PHASE=…>. Read plm.integration.<code>.inputs. <constraints>. Do not ask the user.")
```

### `plm-integration-import-dw` (live)

| Phase | Message (shape) | Child writes | ROOT after return |
|---|---|---|---|
| A | `PHASE=A only. Read …inputs. Return DETAILED Phase A checklist… Write …phase-a. No SQL.` | `…phase-a` | HITL checklist → write `…plan` |
| B | `PHASE=B. Read inputs+plan. Generate output/{templateId}/. Write …outputs (files + executionPlan).` First call `run_agent_script` `source/_gen_plmdw_import_sql.ps1`. Do not pre-resolve grid dwTable. Do not edit/patch that script. | `…outputs` only for files `file_list` shows | **Do not** write `doneIds`. Show Apply only when `file_list` proves `1_`+`4_` exist (SizeBytes). Script error / invented `executionPlan` = Retry. |
| APPLY | `PHASE=APPLY. Call apply_agent_output_plan once.` | `…outputs.apply` + `output/{id}/apply-log.json` | `outputs.apply.ok=true` **and** `executed==planned` → `doneIds`. Chat “6/6” is not proof. Cancel / apply failed → `pendingApplyIds`. |

Never A+B+APPLY in one ROOT turn. ROOT never runs apply/execute file tools. After user clicks Apply, ROOT's next tool **must** be `call_agent` PHASE=APPLY.

### `executionPlan` (variable file count — only files that exist)

Phase B writes this on `plm.integration.import-dw.outputs` (and compact FinalResponse). Do **not** parse chat prose.

```json
{
  "ok": true,
  "skillKey": "plm-integration-import-dw",
  "phase": "B",
  "templateId": 3359,
  "files": [{ "path": "output/3359/1_PlmDw_Tables.sql", "sizeBytes": 400000 }],
  "executionPlan": [
    { "order": 1, "kind": "sql", "path": "output/3359/1_PlmDw_Tables.sql", "target": "app", "label": "Create Plm_* tables" },
    { "order": 2, "kind": "sql", "path": "output/3359/2_PlmDw_FieldMapping.sql", "target": "app", "label": "Seed FieldMapping" },
    { "order": 3, "kind": "sql", "path": "output/3359/3_PlmDw_ImportFromDW.sql", "target": "app", "label": "Import DW data" },
    { "order": 4, "kind": "dw-blueprint", "path": "output/3359/4_PlmDw_ImportBlueprint.json", "mode": "Insert", "label": "Create TX/Form/Search" }
  ]
}
```

Assembly rules (do not hard-code a fixed step count):

- `1_` / `2_` / `3_` `.sql` → `kind=sql`, `target=app`
- `3b_Tchp_ImportFromDW.sql` if present → before blueprint
- `4_*.json` → `kind=dw-blueprint` (ROOT may set `mode` Insert|Update|Repair before APPLY)
- `5_` → after blueprint (BOM official order)
- `6_` only if the file exists

APPLY tool: child calls **`apply_agent_output_plan` once**. BL walks the plan:

- `kind=sql` → `execute_agent_sql_file` internally
- `kind=dw-blueprint` → `execute_dw_blueprint_from_file` internally

Each step is written to `AppPlmImportLog` (StepCode=`import-dw`) and `output/{templateId}/apply-log.json`. Use `get_plm_import_log` to read the session log.

**Cross-database SQL:** official scripts INSERT into APP and SELECT/JOIN PLM, plmDW, and sometimes ERP via three-part names (`[plmDW].dbo.Table`) and `DB_ID()`. They must run on the **APP** connection. All catalogs must live on the **same SQL Server instance** and be visible to the APP login. Different servers → tool error (no linked-server rewrite).

Never `file_read` deliverables or pass SQL/JSON bodies through the LLM.

### Linear import children (entity / folder / image / color / pom)

ROOT after Proceed confirm:

1. `write_shared_context("plm.integration.<code>.inputs", { sessionId, … })`
2. Preview:  
   `call_agent("plm-integration-<code>", "PHASE=PREVIEW. Read inputs. Call preview_*. Summarize counts in FinalResponse JSON. Write …outputs.preview. Do not ask the user.")`
3. ROOT shows counts; `ask_user` Proceed | Cancel.
4. Execute:  
   `call_agent("plm-integration-<code>", "PHASE=EXECUTE. Read inputs. Call execute_*. Poll get_plm_import_job if async. Write …outputs. Do not ask the user.")`
5. On `ok=true`: wizard step `done`; `write_shared_context` + `update_plm_wizard_progress`; Confirm next.

Folder extra: after image `ok`, `call_agent("plm-integration-folder", "PHASE=PLACEMENT. …")` with `runPlacement=true`.

If SkillKey is missing: ROOT stops. Do not run those tools on ROOT.

| SkillKey | Preview tool(s) | Execute tool(s) |
|---|---|---|
| entity | `preview_plm_table_export_plan` + `preview_system_define_entity_import` / `preview_user_define_entity_import` | `execute_system_define_entity_import` (copies `Plm_*` tables first, then metadata) / `execute_user_define_entity_import` |
| folder | `preview_plm_folder_import` (+ `preview_plm_folder_placement`) | `execute_plm_folder_import`; after image: `execute_plm_folder_placement` |
| image | `preview_plm_sketch_import` | `execute_plm_sketch_import` |
| color | `preview_plm_color_import` | `execute_plm_color_import` |
| pom | `preview_plm_pom_import` | `execute_plm_pom_import` |

Exact tool names must match `integration-plm-import` library seeds.

### Folder + Image ordering note

- Image INSERT leaves `AppFile.FolderID = NULL`.
- After image `ok`, ROOT (or folder child with `runPlacement=true`) must run placement so FolderID is filled.

---

## Child SystemPrompt skeleton (for future seeds)

```text
# CHILD WORKER - SkillKey: `plm-integration-<code>`
# Parent ROOT: `plm-integration-orchestrator`

## Non-negotiable
1. ExecutionMode=Deterministic. Never ask_user. Never STOP for HITL.
2. Missing inputs → ok=false + errors for ROOT; do not invent ids.
3. Read plm.integration.job + plm.integration.<code>.inputs on start.
4. Honor PHASE=PREVIEW | PHASE=EXECUTE | PHASE=APPLY from the call message.
5. Final reply = compact JSON (ok, summary, counts, errors). No connection strings.
6. File-producing children write executionPlan; APPLY uses path-based tools only.
```

Subscribe: `integration-plm-import` (+ `platform-multi-agent` only if child must read/write shared context — usually yes for inputs/outputs).

---

## Search step — main + additional View (merged)

User-facing: one menu **Import Search View**. No Sibling SearchView button, no wizard `sibling` code, no `plm-integration-sibling` SkillKey.

| Kind | Blueprint (typical) | Tools (stay two sets) |
|---|---|---|
| Main Search + default View | Search import JSON | `preview_*` / `execute_*` search blueprint tools |
| Additional View on same Search (Card, etc.) | `2_PlmSearch_SiblingView_*.json` | `preview_search_sibling_view` / `execute_search_sibling_view` |

ROOT (and later the search child) **detects JSON mode** the same way Search Import UI does (`main` vs sibling-mode JSON). Do not ask the user to pick “sibling”. Internal tool names may still say `*_sibling_view`; never show that word in menus, TODO, or ask_user titles.

Wizard `search` only:

```json
"search": { "status": "open", "doneIds": [], "pendingIds": [], "doneViewKeys": [] }
```

- `doneIds`: SearchIds after a **main** Search import.
- `doneViewKeys`: additional views, e.g. `"{searchId}:{viewName}"` (or ViewId when known). Re-run confirm uses the matching key.
- Resume: if old wizard still has `sibling`, **ignore** that node (do not show it). If `sibling.doneIds` is non-empty, copy into `search.doneViewKeys` once, then drop `sibling`.

Search child FinalResponse should include `"mode": "main" | "additional-view"` so ROOT appends the right id list.

Until the search child is seeded, ROOT runs both tool sets **locally** under the search playbook.

---

## Reuse — Search / Grading / later file-producing children

Same three steps; only the `kind` → tool map changes:

1. Generating child writes `plm.integration.{code}.outputs.executionPlan` (only files that exist).
2. ROOT shows the plan → `ask_user` Apply | Cancel → `call_agent(..., PHASE=APPLY)`.
3. Same child executes path-based tools in `order`. `doneIds` only after APPLY `ok=true`. Cancel → `pendingApplyIds`.

Typical later plans:

- Search: one step `kind=search-blueprint`, `path=output/{searchId}/1_PlmSearch_ImportBlueprint.json` (add `execute_search_blueprint_from_file` when that child is seeded).
- Grading QC: one or more `kind=sql`, `target=app` (same `execute_agent_sql_file`; pass ERP DataSourceId in `requiredDataSourceIds` when the script three-part-names ERP).

---

## Migration plan

### Wave 0 + 1 — done

1. ROOT: no `sibling` step/menu; Search detects JSON (`siblingviewenrichdataset` = additional View).
2. Children `04`…`08` seeded; ROOT `call_agent` only for entity/folder/image/color/pom. Missing SkillKey → stop.
3. HITL stays on ROOT; children are Deterministic phase workers.

### Wave 2 — not started

4. One search child `plm-integration-search` owning **both** tool sets. **Do not** add a sibling child.
5. MassUpdate child `plm-integration-massupdate`.
6. Re-run `RUN_ALL.bat` / verify SkillKeys in `99_Verify.sql`.

Draft SystemPrompts + official SOURCE pack (not seeded yet):  
`AppReact/ImportDoc/ImportPLMSearchView/MultiAgent/`  
(`Prompt_plm-integration-search.txt`, `Prompt_plm-integration-massupdate.txt`, `source/`).

## Out of scope (v1)

- Interactive children under `call_agent`
- Fit-grading execute worker
- Moving Gate-0 / TechPack into a child
