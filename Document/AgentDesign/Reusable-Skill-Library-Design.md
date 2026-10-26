# Reusable Skill Library — Design (AppAISkill)

| | |
|---|---|
| **Status** | **Frozen (S0 approved)** — implementation may proceed per §3 |
| **Date** | 2026-10-26 |
| **Language** | English (canonical) |
| **Owner surface** | `AppAISkill` + `AppAISkillRef`; Agent bind via `AppAgentSkillSetSkill` |
| **Related (separate)** | `AppAISkill-FromUserPrompt-Design.md` — narrower From-Prompt draft; implement as **S1** under this plan |
| **Related systems** | BlueCherry MCP Skills Admin / SkillLoader / Agent Skills tab; App `AISkillManagement`, `AppAgentSkillSet`, GenericAgent |

Changing storage table, Agent-vs-Chat binding, or runtime load model requires an explicit design revision — do not reinterpret in code reviews.

---

## 1. Problem and goals

### 1.1 Gap

MCP lets admins create skills and lets chat capture workflows into skills; other sessions load instructions + resources via progressive disclosure (index → full body).

The App already has Agent Tool/MCP/Child registration and an `AppAISkill` library UI, but GenericAgent runtime barely uses `AppAISkill`. Missing: Save Chat as Skill, per-Agent Available Skills, context-driven load.

### 1.2 Goals

1. Create a Skill (admin), or save the current chat as a Skill (LLM-authored).  
2. Reuse Skill prompt + text resources across conversations.  
3. Configure **Available Skills** on the Agent (SkillSet), inherited by Chat.  
4. Runtime progressive disclosure: light index always; full body via `load_app_skill` — never dump all SkillContent into SystemPrompt.  
5. Store structured instructions + reference resources, not raw chat dumps as the skill body.

### 1.3 Non-goals (v1)

- Cross-tenant skill marketplace  
- Binary blobs (images, PDF) in `AppAISkillRef`  
- Replacing `AppAgentSkillSet` with skills  
- Auto-creating an Agent from a Skill  
- Binding Private skills onto shared Agents  
- Session-level Skill picker chips (defer; Agent index is enough for v1)

---

## 2. Concepts (MCP ↔ App)

| MCP | App |
|-----|-----|
| Skill name + description (index) | `AppAISkill.Name` + `Description` |
| SKILL.md / instructions | `AppAISkill.SkillContent` |
| Resource files under skill folder | `AppAISkillRef` (FileName + FileContent) |
| SkillLoader list → load full | Skill index + built-in `load_app_skill` |
| Admin New / Save Skill (+ AI Generate) | `AISkillManagement` |
| Agent modal → Skills tab (Add Skill, always / on demand) | `AgentSkillSetManagement` → **Skills** tab |
| Chat NL (“turn this into a skill”) + admin create tool | Chat NL / composer chip → `save_as_app_skill` |
| Tools | `AppAgentToolRegister` / MCP — **Skill ≠ Tool** |

**Principle:** Skill = reusable knowledge / procedure; Tool = executable capability; Agent SkillSet = persona + capability boundary.

---

## 3. Phased delivery (locked order)

| Phase | Deliverable | Ships |
|-------|-------------|--------|
| **S0** | This frozen design | Done |
| **S1** | Admin Skill CRUD polish + From Prompt / AI Generate | `AISkillManagement` |
| **S2** | Save Chat as Skill (`save_as_app_skill` + composer chip); Private columns on `AppAISkill` | Capture without Agent reuse |
| **S3** | Agent **Skills** tab + `AppAgentSkillSetSkill` | Binding |
| **S4** | Runtime index injection + `load_app_skill` (+ honor `LoadMode`) | Reuse in chat |
| **S5** | Resource polish (session-file links, promote Private → Tenant) | Optional |

S1 and S2 may run in parallel. **S3 before S4.** S1/S2 must not wait on S3.

---

## 4. Storage

### 4.1 Primary tables (existing)

Reuse **`AppAISkill` + `AppAISkillRef`**. Do **not** store reusable skills in `AppAgentSkillSet`. Ignore legacy `AppUserSkill` / `AppUserSkillList`.

```text
AppAISkill
  SkillId, Name (UNIQUE), Description, SkillContent, IsActive, CreatedDate, UpdatedDate
  -- added in S2:
  OwnerUserId     NULL = tenant-owned; else creator
  Visibility      'Tenant' | 'Private'
  SourceType      'Manual' | 'FromPrompt' | 'FromChat'

AppAISkillRef
  RefId, SkillId, FileName, FileContent, SortOrder, CreatedDate
```

### 4.2 Agent bind table (S3)

```sql
CREATE TABLE dbo.AppAgentSkillSetSkill (
    Id         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SkillKey   NVARCHAR(100) NOT NULL,
    SkillId    INT NOT NULL,
    SortOrder  INT NOT NULL CONSTRAINT DF_AppAgentSkillSetSkill_Sort DEFAULT (0),
    IsActive   BIT NOT NULL CONSTRAINT DF_AppAgentSkillSetSkill_Active DEFAULT (1),
    LoadMode   NVARCHAR(20) NOT NULL CONSTRAINT DF_AppAgentSkillSetSkill_LoadMode DEFAULT ('on_demand'),
    CONSTRAINT UX_AppAgentSkillSetSkill UNIQUE (SkillKey, SkillId)
);
```

- `on_demand` (default): Name+Description in index; full body only after `load_app_skill`.  
- `always`: composed SkillContent(+refs) appended to agent instructions for that skill (use sparingly).

### 4.3 What a Skill stores

| Store | Content |
|-------|---------|
| Required | Name, Description, SkillContent (Markdown), IsActive |
| Refs (text) | `references/*`, `examples/*` — composed on load |
| Audit refs (not loaded) | `_source_prompt.txt`, `_source_chat.md`, `_meta.json` |
| Not in Skill | Large CSV/binary/images; secrets; ToolRegister; one-off result grids / PPT/PDF |

v1: text-only in `AppAISkillRef`. Large files → session files / object storage references only.

Compose via existing `AppAISkillBL.GetComposedSkillPrompt`, skipping Ref names with `_` prefix.

---

## 5. User experience

### 5.1 Admin — Create / Edit Skill (S1)

**Entry:** `AISkillManagement` (MCP Admin Skills parity)

- New Skill / edit → Save Skill  
- Upload/paste text → `AppAISkillRef`  
- From prompt / AI Generate → draft Name / Description / SkillContent → user saves as **Tenant**

### 5.2 Chat — Save as Skill (S2)

**Default scope:** **Current task arc** — last user message *before* the save request through that request’s assistant completion (+ optional Think/plan). LLM rewrites into Role / Steps / Constraints / Outputs. Not a raw transcript dump.

| Scope | Default |
|-------|---------|
| Current task arc | **Yes** |
| Selected turns | Admin/power review path only |
| Full chat (summarized) | Only if user explicitly asks in the same message |

**Chat entry (MCP-aligned primary path):**

1. User types `SAVE AS SKILL` / `save as skill` / “turn this into a skill” (optional name in message).  
2. Or composer chip **Save as Skill** inserts/sends that phrase (no multi-step wizard in composer).  
3. Agent calls built-in **`save_as_app_skill`**.  
4. Server: load task arc → LLM draft → INSERT **Private** `AppAISkill` (`SourceType=FromChat`).  
5. Agent replies with Name / SkillId (and short preview).  

**Defaults:**

- Visibility = Private; `OwnerUserId` = current user  
- No auto-bind to Agent Available Skills  
- Name collision → suffix `-2`, `-3` (or one `ask_user` if name missing/ambiguous)  
- Optional `_source_chat.md` Ref when requested  

**Admin/power review modal:** optional edit-before-save (Selected turns, Update existing, attach refs). Not the primary chat path.

### 5.3 Agent — Skills tab (S3) — in scope

**Entry:** `AgentSkillSetManagement` → **Skills** tab (MCP Agent Skills tab parity)

- List bound Tenant skills; **＋ Add Skill** picker  
- Per row: `on_demand` | `always`; remove; sort  
- Chat config: **read-only** list of Agent’s Available Skills  

**Why required for full product:** without bind UI, either every Tenant skill is Available (noise) or saved skills never load in Agent chat. S1–S2 may ship capture-only; **S3 is required before S4**.

### 5.4 Runtime (S4)

1. Inject Available Skills **index** (Name + Description + SkillId) for `on_demand` (and always) rows.  
2. For `always`, also inject composed body.  
3. Built-in **`load_app_skill`**(name|id): only Agent-bound, Active, Tenant skills; return composed text; respect `MaxToolResultChars`.  
4. Never silently inject all SkillContent for every skill.

System hint when tools are registered: on save-as-skill intent, call `save_as_app_skill`; when a listed skill is relevant, call `load_app_skill`.

---

## 6. Permissions (locked)

| Action | Who | Result |
|--------|-----|--------|
| Save as Skill (chat / chip) | Any user of that Agent Chat | **Private** skill |
| Create/edit Tenant skills | Existing AISkill admin | **Tenant** library |
| Bind Available Skills | Same as editing that Agent | Join rows only |
| `load_app_skill` | Runtime | Bound **Tenant** skills only (v1) |

Private skills appear under “Mine” for the owner. Promoting Private → Tenant is an admin action (S5). Binding Private to a shared Agent is **out of scope for v1**.

---

## 7. API and BL

| Surface | Role | Phase |
|---------|------|-------|
| Existing AISkill CRUD | Keep | — |
| `POST …/AISkill/DraftFromPrompt` | Admin AI Generate | S1 |
| `POST …/AISkill/DraftFromChat` | Draft DTO (review modal) | S2 |
| `POST …/AISkill/SaveDraft` | Persist after review | S2 |
| Built-in `save_as_app_skill` | Chat: draft task arc + persist Private | S2 |
| `GET/PUT …/AgentSkillSet/{key}/AvailableSkills` | Bind list + LoadMode | S3 |
| Built-in `load_app_skill` | Runtime compose + ACL | S4 |

BL:

- `AppAISkillBL` — DraftFromChat, SaveFromChat, compose skips `_` refs  
- `AppAgentSkillSetSkillBL` — binding CRUD  
- `GenericAgentEngine` — register `save_as_app_skill` / `load_app_skill`; inject index / always bodies  

---

## 8. Frozen decisions

| # | Decision |
|---|----------|
| 1 | Store reusable skills in **`AppAISkill` + `AppAISkillRef`**, not `AppAgentSkillSet` |
| 2 | Available Skills configured on **Agent**; Chat inherits (read-only) |
| 3 | Runtime = **index + `load_app_skill`**; `LoadMode` always \| on_demand |
| 4 | Save Chat default scope = **current task arc** |
| 4.1 | LLM authors SkillContent; tool/API persists |
| 4.2 | UI mirrors MCP: Chat NL + composer chip; Admin New/Save Skill; Agent Skills tab; review modal = power path |
| 5 | v1 Refs = **text only**; large files by reference |
| 6 | Chat save → **Private**; Tenant library + Agent bind → **admin** |
| 7 | This document is canonical English design; FromUserPrompt doc stays separate (S1 subset) |
| 8 | **Agent Skills tab is in scope** at S3; do not block S1/S2 |

---

## 9. Acceptance criteria

1. Admin New/Save Skill (+ AI Generate) creates Tenant skills with optional Refs; Description visible in list.  
2. Chat `SAVE AS SKILL` / synonym / composer chip → `save_as_app_skill` → Private skill from task arc; reply includes Name/SkillId.  
3. Agent Skills tab: Add Skill; `on_demand` (default) / `always`; unbound skills cannot load.  
4. New sessions see index only for `on_demand` until `load_app_skill`.  
5. Saving a Skill never changes ToolRegister and never auto-binds Available Skills.  
6. Compose never injects `_source_*` / `_meta.json` Refs.

---

## 10. Implementation kickoff

Proceed in order **S1 → S2 → S3 → S4** (S1∥S2 allowed). First code milestone: S2 Private columns migration + `save_as_app_skill` + composer chip, or S1 From Prompt — either is valid after this freeze.
