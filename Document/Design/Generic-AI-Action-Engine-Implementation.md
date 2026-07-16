# Generic AI Action Engine — Implementation Summary

**Status:** Shipped (Phase 1 + 2 + 3 complete)  
**Date:** 2026-07-15  
**Scope:** AppAI Platform — zero-code AI capability engine

---

## What Was Built

A **platform-level AI engine** that lets administrators deliver any AI feature — image analysis, text generation, structured data extraction — by combining a prompt template with form configuration. No new code is needed per feature.

### Core Principle

```
AppAISkill (prompt)  +  CommandActionButton config  =  Complete AI feature
```

One engine handles all use cases. The platform team builds it once; administrators self-serve new features indefinitely.

---

## Architecture

```
Admin configures (one-time per feature):
  ┌─────────────────────┐     ┌─────────────────────────────────┐
  │  AI Skill Library   │     │  Command Action Editor          │
  │  ─────────────────  │     │  ───────────────────────────    │
  │  Name: FabricBom    │ ──► │  ActionType = 56 (AI_ACTION)   │
  │  Prompt: "Extract   │     │  skillName → skill name         │
  │  fabric BOM..."     │     │  inputBindings → form fields    │
  └─────────────────────┘     │  outputBindings → grids/fields  │
                               └─────────────────────────────────┘
                                              │
User clicks button at runtime:               ▼
  OneLayoutItem.tsx
    → reads inputBindings from form state (DictOneToOneFields)
    → POST /api/ai/action  { inputs, skillName }
    → AiActionService resolves prompt, calls AI provider
    → auto-maps AI response keys → DB column names
    → writes to DictOneToManyFields (grid) or DictOneToOneFields (field)
    → standard platform Save persists to database
```

---

## Files Created / Modified

### Backend (AppAI.Web)

| File | Change | Purpose |
|------|--------|---------|
| `Services/IAiActionService.cs` | **Created** | Interface + record types (`AiActionInput`, `AiActionRequest`, `AiActionResult`) |
| `Services/AiActionService.cs` | **Created** | Multi-provider AI engine — Anthropic, OpenAI, Gemini; prompt resolution; markdown fence stripping |
| `Endpoints/AiActionEndpoints.cs` | **Created** | `POST /api/ai/action` — converts base64 → bytes, calls service |
| `Program.cs` | **Modified** | Register `IAiActionService` + map `AiActionEndpoints` |

### Frontend (AppReact)

| File | Change | Purpose |
|------|--------|---------|
| `webapi/aiActionSvc.ts` | **Created** | `callAiAction()`, `parseImageFieldValue()`, `fileToBase64()` |
| `…/MasterDetailFlexLayoutForm/OneLayoutItem.tsx` | **Modified** | AI action handler for `CommandActionButton` (ActionType=56); auto-map helpers |
| `…/CommandEditorPart/AiActionConfigSection.tsx` | **Created** | Visual config editor — skill picker, input/output binding lists, JSON preview |
| `…/CommandEditor.tsx` | **Modified** | Added `CollapsibleSection` for ActionType=56 → renders `AiActionConfigSection` |

---

## Key Implementation Details

### Backend — AiActionService

- Resolves `AppAISkill` by name via `AppAISkillBL.GetSkillByName()` + `GetComposedSkillPrompt()`
- Provider selected via `LLMProviderHelper.GetConfiguredProvider()` (reads `appsettings.json`)
- Multi-modal content blocks per provider:
  - **Anthropic**: `{ type: "image", source: { type: "base64", media_type, data } }`
  - **OpenAI**: `{ type: "image_url", image_url: { url: "data:mime;base64,..." } }`
  - **Gemini**: `system_instruction` for prompt, `inline_data` for images
- Strips markdown fences (` ```json `) from LLM response before returning

### Frontend — Runtime Handler (OneLayoutItem.tsx)

**ActionAttribute parse** — handles both string (legacy) and object (editor writes object directly):
```ts
const cfg = (() => {
  try {
    const raw = cmdAction.ActionAttribute ?? cmdAction.actionAttribute;
    if (!raw) return {};
    return typeof raw === 'string' ? JSON.parse(raw) : raw;
  } catch { return {}; }
})();
```

**Image field reading** — form stores images as data URLs:
```ts
// DictOneToOneFields["ProductImage"] = "data:image/jpeg;base64,/9j/..."
parseImageFieldValue(raw) → { base64: "...", mimeType: "image/jpeg" }
```

**Auto-map (Phase 3)** — resolves AI-friendly keys to DB column names at runtime using form field metadata:
```ts
// AI returns:   { materialName: "Cotton Twill" }
// Auto-map to:  { Fabric_Name:  "Cotton Twill" }  ← from AppTransactionFieldList

const lookup = buildDbNameLookup(transactionExDto, unitId);
// lookup["materialname"] → "Fabric_Name"
// lookup["fabricname"]   → "Fabric_Name"  (DisplayName also indexed)

const mappedRows = aiRows.map(row => mapRowToDbColumns(row, lookup));
```
Unrecognized keys pass through unchanged (safe fallback).

### Frontend — Visual Config Editor (AiActionConfigSection.tsx)

Appears in Command Action Editor when `ActionType = 56`. Three sections:
1. **Skill picker** — dropdown populated from `aiSkillSvc.GetAll()`
2. **Input Bindings** — field selector (from hierarchy unit list) + image/text type
3. **Output Bindings** — AI key name + target type (child_grid / text_field) + target field/unit picker
4. **JSON Preview** — live read-only preview of the generated `ActionAttribute`

Writes directly to `action.ActionAttribute` as a plain object (same pattern as `LinkToUI`, `IsShowOnTopMenu` etc. in `CommandBasicOptions`).

---

## ActionAttribute Schema

```json
{
  "skillName": "FabricBomExtract",
  "inputBindings": [
    { "fieldName": "ProductImage", "inputType": "image" },
    { "fieldName": "StyleCategory", "inputType": "text" }
  ],
  "outputBindings": [
    { "outputKey": "rows",        "targetType": "child_grid", "targetName": "<unitId>" },
    { "outputKey": "description", "targetType": "text_field", "targetName": "StyleDescription" }
  ]
}
```

| Field | Type | Values |
|-------|------|--------|
| `skillName` | string | Name of an `AppAISkill` record |
| `inputBindings[].fieldName` | string | DB column name in `DictOneToOneFields` |
| `inputBindings[].inputType` | string | `"text"` \| `"image"` |
| `outputBindings[].outputKey` | string | Key name in AI JSON response |
| `outputBindings[].targetType` | string | `"text_field"` \| `"child_grid"` |
| `outputBindings[].targetName` | string | Field name or unit ID (numeric string) |

---

## Admin Setup Guide (No Code Required)

### Step 1 — Create an AI Skill

Go to **AI Skill Management** → New Skill:
- **Name**: e.g. `FabricBomExtract`
- **SkillContent**: System prompt instructing the AI to return a specific JSON shape

Example prompt for Fabric BOM:
```
You are a fashion industry expert. Analyze the provided input and extract
a complete fabric Bill of Materials.

Return ONLY valid JSON:
{
  "rows": [
    {
      "materialName": "Shell",
      "placement": "Main body",
      "fabricType": "Woven",
      "fiberCompositions": "80% Cotton / 20% Polyester",
      "color": "Navy Blue",
      "confidenceScore": 0.85
    }
  ],
  "warnings": ""
}

Rules:
- care_label: extract fiber percentages EXACTLY as printed
- Keep each component separate; never merge shell and lining
- confidenceScore: 1.0 = explicitly labeled, 0.7–0.9 = visually inferred
```

### Step 2 — Configure the Button in Form Builder

1. Open the target transaction → **Application Builder**
2. Drag a **CommandActionButton** onto the form layout
3. In the Command Action Editor:
   - Set **Operation Type** = `AI Action (56)`
   - The **AI Action Configuration** section appears automatically
   - Pick the skill from the dropdown
   - Add input bindings (which form fields to send to AI)
   - Add output bindings (where to put the AI response)
4. Set **DisplayName** = "AI Generate BOM" (or any label)
5. Save

### Step 3 — Test

Click the button on the live form. The spinner shows during AI processing. Results populate the configured fields/grids automatically. Save normally.

---

## Use Case Library

| Feature | Input | Skill | Output |
|---------|-------|-------|--------|
| Fabric BOM extraction | `ProductImage` (image) | `FabricBomExtract` | `rows` → child_grid |
| Style description | `Category`, `Color`, `Season` | `StyleDescriptionGen` | `description` → text_field |
| Care instructions | `FiberComposition` | `CareInstructionGen` | `careText` → text_field |
| Quality defect detection | `ProductPhoto` (image) | `QualityDefectDetect` | `defects` → child_grid |
| Garment cost estimate | `FabricType`, `Weight`, `Market` | `CostEstimator` | `estimatedCost` → text_field |
| Hang tag / label reader | `HangTagPhoto` (image) | `HangTagReader` | `rows` → child_grid |
| Compliance risk check | `MaterialData`, `TargetMarket` | `ComplianceRiskCheck` | `flags` → child_grid |

All rows above require **zero code changes** — only a new `AppAISkill` record and a form configuration.

---

## Verification Checklist

### Backend
- [ ] `dotnet build AppAI.Core.sln` — 0 errors
- [ ] `POST /api/ai/action` with `{ skillName: "...", inputs: [...] }` returns `200` with `rawJson`
- [ ] Response `rawJson` parses as valid JSON

### Frontend
- [ ] Command Action Editor shows **AI Action Configuration** section when ActionType = 56
- [ ] Skill dropdown populates from AI Skill Management data
- [ ] Input/output binding rows add and remove correctly
- [ ] JSON Preview updates in real time as config changes
- [ ] Save persists configuration; reload shows same config

### End-to-end (Fabric BOM)
- [ ] Open Style form → click AI button → spinner appears
- [ ] Care label image → returned rows match label fiber percentages exactly
- [ ] Product photo → multiple rows (shell, lining, trims, etc.)
- [ ] AI-returned keys correctly mapped to database column names (not raw AI key names)
- [ ] Edit a row manually → Save → rows persist in DB

### Generics validation
- [ ] Create a second skill + second button on a different form → works without code change

---

## What Was Explicitly NOT Built

- No BOM-specific React component
- No new API controller
- No plugin or external assembly
- No new npm or NuGet packages
- No database schema migration (ActionAttribute already existed as `dynamic`)
- No deployment step between new AI features

---

## Related Files (Reference)

| File | Role |
|------|------|
| `AppAI.Web/Services/LLMOcrService.cs` | Template this engine was modelled after |
| `APP.BL/AppMgr/AiSkill/AppAISkillBL.cs` | Skill lookup — `GetSkillByName`, `GetComposedSkillPrompt` |
| `APP.BL/DbGenie/LLMProviderHelper.cs` | Provider + API key resolution |
| `APP.Components.Dto/UserDefine/DbGenie/DbGenieDto.cs` | `EmLLMProvider` enum |
| `AppReact/src/webapi/aiSkillSvc.ts` | Frontend skill management API |
| `Document/Design/AI-Action-Engine-Showcase.html` | Visual architecture showcase (open in browser) |
