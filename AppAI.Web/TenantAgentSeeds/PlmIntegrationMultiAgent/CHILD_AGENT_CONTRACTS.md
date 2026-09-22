# PLM Multi-Agent — Child SkillKey / call_agent contracts (draft)

**Status:** Draft for ROOT ↔ child split. Only `plm-integration-import-dw` is seeded today.  
**ROOT:** `plm-integration-orchestrator` (Interactive, all HITL).  
**Children:** Deterministic, never `ask_user` / PlanGate wait.

## Platform rules (non-negotiable)

| Rule | Detail |
|---|---|
| HITL only on ROOT | Child must not call `ask_user`. Missing input → structured error JSON for ROOT. |
| call_agent is headless | New empty history each call; pass phase + ids in message; durable facts in shared context / session. |
| Shared context | Same WorkflowId as ROOT for the live turn. After restart, prefer `AppPlmImportSession` (`get_plm_wizard_progress`) + re-write shared keys. |
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
| `plm-integration-entity` | `entity` | Draft | Linear once |
| `plm-integration-folder` | `folder` | Draft | Linear skippable (+ placement) |
| `plm-integration-image` | `image` | Draft | Linear skippable |
| `plm-integration-color` | `color` | Draft | Linear skippable |
| `plm-integration-pom` | `pom` | Draft | Linear skippable |
| `plm-integration-search` | `search` | Later | Repeatable by SearchId |
| `plm-integration-sibling` | `sibling` | Later | Repeatable |
| `plm-integration-massupdate` | `massupdate` | Later | Repeatable |

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
| B | `PHASE=B. Read inputs+plan. Generate output/{templateId}/. Write …outputs.` | `…outputs` | Append TemplateId to wizard `import-dw.doneIds`; `update_plm_wizard_progress` |

Never A+B in one ROOT turn.

### Linear import children (draft — same shape)

ROOT after Proceed confirm:

1. `write_shared_context("plm.integration.<code>.inputs", { sessionId, … })`
2. Optional preview turn:  
   `call_agent("plm-integration-<code>", "PHASE=PREVIEW. Read inputs. Call preview_*. Summarize counts in FinalResponse JSON. Write …outputs.preview. Do not ask the user.")`
3. ROOT shows counts; `ask_user` Proceed | Cancel.
4. Execute:  
   `call_agent("plm-integration-<code>", "PHASE=EXECUTE. Read inputs. Call execute_*. Poll get_plm_import_job if async. Write …outputs. Do not ask the user.")`
5. On `ok=true`: wizard step `done`; `write_shared_context` + `update_plm_wizard_progress`; Confirm next.

Until child SkillSets exist, ROOT runs the same preview/execute tools **locally** (current prompt playbooks).

| SkillKey | Preview tool(s) | Execute tool(s) |
|---|---|---|
| entity | `preview_system_define_entity_import` / `preview_user_define_entity_import` | `execute_system_define_entity_import` / `execute_user_define_entity_import` |
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
4. Honor PHASE=PREVIEW | PHASE=EXECUTE from the call message.
5. Final reply = compact JSON (ok, summary, counts, errors). No connection strings.
```

Subscribe: `integration-plm-import` (+ `platform-multi-agent` only if child must read/write shared context — usually yes for inputs/outputs).

---

## Migration plan (when implementing)

1. Add `04_Seed_PlmIntegrationEntity_Child.sql` … `08_…Pom_Child.sql` (INSERT-only, like DW child).
2. Point ROOT catalog "How to run" column at `call_agent` for those codes.
3. Keep preview→confirm→execute HITL on ROOT; children stay Deterministic phase workers.
4. Re-run `RUN_ALL.bat` / verify SkillKeys appear in `99_Verify.sql`.

## Out of scope (v1)

- Interactive children under `call_agent`
- Fit-grading execute worker
- Moving Gate-0 / TechPack into a child
