# AppAISkill — From User Prompt to Skill / Config — Design

**Status:** Design only (implement later)  
**Date:** 2026-10-20  
**Related:** `ThinkMode-ToolsPrefer-Plan.md` (Items 1–3); GenericAgent Architecture §11–12; existing `AISkillManagement.tsx` / `AppAISkillBL`

---

## 1. Problem

Users (and admins) often paste a long **natural-language request** (what the agent should do, data sources, outputs). Today that text is either:

- Typed into chat as a one-off user message, or  
- Manually rewritten into `AppAISkill.SkillContent` / `AppAgentSkillSet.SystemPrompt`

There is no first-class flow: **summarize / structure a user prompt → create or update an `AppAISkill` row (and optionally feed Agent SkillSet config).**

---

## 2. Scope clarification: two tables

| Table | Role today | This design |
|-------|------------|-------------|
| **`AppAISkill`** | Flat prompt library (name, description, content, file refs). Used by DbGenie / integration catalogs; **not** the GenericAgent persona table. | **Primary target:** generate / update reusable prompt snippets from a user brief |
| **`AppAgentSkillSet`** | Agent persona: SystemPrompt + tools + ExecutionMode + flags | **Secondary / later:** optional “Apply to Agent” that merges summarized policy into SystemPrompt or opens Skill Designer with recommended tools |

Do **not** collapse the two tables. Architecture already decided against extending `AppAISkill` into a full agent (§11). This feature **enriches AppAISkill** and may **hand off** to Agent SkillSet design (existing tool-recommend flow in Architecture §12).

---

## 3. Goals

1. User (or admin) pastes a **raw prompt / use-case brief**.  
2. System produces a **structured skill draft**: Name, Description, SkillContent (cleaned system-style instructions), optional References list.  
3. User **reviews / edits**, then **Save as new** or **Update existing** `AppAISkill`.  
4. Optional later: “Use for Agent…” → jump to `AppAgentSkillSet` wizard with recommended tools (reuse §12 pipeline).

Non-goals (v1):

- Auto-subscribe MCP tools without review  
- Replace Agent SkillSet AI edit (`EditSystemPrompt`)  
- Multi-tenant skill marketplace  

---

## 4. User experience (proposed)

### 4.1 Entry points

| Entry | Where | Action |
|-------|--------|--------|
| A | `AISkillManagement` — toolbar **“From prompt…”** | Modal: paste brief → Generate → review → Save |
| B | GenericAgent chat (optional later) | “Save this request as skill” after a good turn — uses last user message + optional Think summary |
| C | Agent SkillSet designer (optional later) | “Import from AppAISkill” picker |

v1 ships **A** only.

### 4.2 Modal flow

```
┌─ Create skill from prompt ─────────────────────────┐
│ Raw user prompt / use case                         │
│ [ multiline                                    ]   │
│                                                    │
│ Options:  ○ New skill   ○ Update existing [pick]   │
│           ☑ Generate description from content      │
│                                                    │
│ [ Generate draft ]                                 │
│ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ │
│ Name        [ ................ ]                   │
│ Description [ ................ ]                   │
│ Skill content (editable)                           │
│ [ large textarea / monaco                      ]   │
│ Warnings: [missing data source | ambiguous …]      │
│                                                    │
│ [ Cancel ]              [ Save to AppAISkill ]     │
└────────────────────────────────────────────────────┘
```

### 4.3 Output shape of “draft”

LLM (or deterministic template) returns JSON:

```json
{
  "name": "Style-Search-Analyze",
  "description": "Run style search and break down by classification, size range, sample status.",
  "skillContent": "## Role\n...\n## Steps\n1. ...\n## Outputs\n...",
  "suggestedTags": ["plm", "search", "analyze"],
  "warnings": ["Confirm which API/table is the style search source."]
}
```

Mapped to `AppAISkillDto`: Name, Description, SkillContent; tags may be unused until a Tags column exists (store in Description footer or skip).

---

## 5. Backend design

### 5.1 API (proposed)

| Method | Route | Body | Result |
|--------|-------|------|--------|
| POST | `/webapi/AISkill/DraftFromPrompt` | `{ RawPrompt, ExistingSkillId? }` | `{ Name, Description, SkillContent, Warnings[] }` |
| (existing) | Create / Update skill | unchanged | Persist after user confirms |

### 5.2 BL

`AppAISkillBL.DraftFromPrompt(dsId, rawPrompt, existing?)`:

1. Validate length (e.g. 50–20k chars).  
2. Call tenant LLM with a **fixed meta-prompt** (not the skill content itself as system):  
   - Extract goal, inputs, steps, constraints, outputs  
   - Write SkillContent as **instructions for an agent**, second person / imperative  
   - Do not invent tool names that are not in a provided catalog snippet (v1: no catalog; warn instead)  
3. Return draft DTO; **no DB write** until Save.

### 5.3 Meta-prompt principles

- Summarize; remove chat fluff.  
- Prefer bullet steps that map to **tools** (align with Tools Prefer plan): fetch → analyze → render/export.  
- Call out open questions as `warnings`, not silent assumptions.  
- If `ExistingSkillId` set: produce a **diff-oriented** rewrite (preserve name unless user asked to rename).

### 5.4 Security

- Same auth as AISkill CRUD.  
- Do not execute tools during draft.  
- Do not embed secrets from RawPrompt into logs.

---

## 6. Optional Phase 2 — bridge to AppAgentSkillSet

After AppAISkill save:

1. Button **“Design Agent from this skill”**  
2. Pre-fill Agent SkillSet:  
   - DisplayName ← skill Name  
   - SystemPrompt ← SkillContent (+ Tools Prefer appendix)  
3. Call existing **tool recommendation** flow (Architecture §12) with SkillContent as use-case text  
4. User reviews RecommendedTools → subscribe libraries  

This reuses catalog retrieval; no new RAG required for v1.

---

## 7. Data model

**v1:** no schema change to `AppAISkill` if Name/Description/SkillContent suffice.

**Optional later columns** (only if product needs them):

| Column | Purpose |
|--------|---------|
| `SourcePrompt` | Audit: original user brief |
| `DraftJson` | Last generator metadata |
| `UpdatedByUserId` | Already may exist via audit elsewhere |

Prefer storing `SourcePrompt` as a Reference file (`AppAISkillRef`) named `_source_prompt.txt` to avoid migration — nice-to-have.

---

## 8. Feasibility

| Area | Rating | Notes |
|------|--------|-------|
| Draft LLM call | High | Same stack as `EditSystemPrompt` on SkillSet |
| UI modal on AISkillManagement | High | Fits existing CRUD page |
| Persist without new tables | High | |
| Auto tool binding | Medium | Defer to Phase 2 + §12 |
| Chat “save as skill” | Medium | Needs session export of user text; after v1 |

---

## 9. Implementation phases (later)

| Phase | Deliverable |
|-------|-------------|
| **D0** | This design approved |
| **D1** | `DraftFromPrompt` API + meta-prompt + unit test on JSON parse |
| **D2** | AISkillManagement modal + Save |
| **D3** | Optional: store source prompt as Ref file |
| **D4** | Bridge to Agent SkillSet + tool recommend |

---

## 10. Acceptance criteria (when built)

1. From AISkillManagement, paste a multi-step user brief → draft Name / Description / SkillContent in &lt; ~30s.  
2. User can edit draft and save; row appears in AppAISkill list.  
3. Update-existing path overwrites content only after confirm.  
4. No agent tools run during draft.  
5. Warnings surface ambiguities (missing source, multiple APIs).

---

## 11. Open questions

1. Should draft SkillContent be English-only or match user language? (Proposal: same language as RawPrompt.)  
2. Max RawPrompt size? (Proposal: 20k chars.)  
3. Who can create skills — all users with AISkill access or admins only? (Follow existing AISkill permissions.)  
4. Relation to Cursor Cloud / Data Integration skill catalogs that also seed AppAISkill — keep generators separate; same table.

---

## 12. Relationship to Think Mode plan

- Think Mode explains **runtime** tool choice for a chat turn.  
- This design captures a **durable** instruction artifact from a user brief.  
- A future “Save Think summary into skill” could append a “Observed procedure” section — not in v1.
