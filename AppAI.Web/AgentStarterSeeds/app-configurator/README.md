# AgentStarter seed — `app-configurator`

**Canonical runtime KB for this agent.** Edit files in this folder directly. Packaged with the web app (`AppAI.Web.csproj` Content → output). On Ensure Form / Search / AppBuilder chat (and EnsureRoot / Agent Management Files list), files sync **PreserveNewest** into:

`FileRepository/Company_{id}/AgentStarter/app-configurator/`

That company folder is what Agent Management → **Files** shows. New Chat copies into chat `source/`.

Do **not** keep agent KB bodies under `Document/` — design plans stay in `Document/AgentDesign/`; human Help stays in `Document/KnowledgeBase/UserGuide/`.

## Files (agent-readable)

| File | Role |
|------|------|
| `commands-knowledge.md` | Agent deep manual (UI + actionType / upsert) — **edit here** |
| `.agent-file-catalog.json` | Path + description injected as catalog summary at run start |

Ops-only (skipped by packaged → AgentStarter sync): `README.md`, `*.ps1`.

## Not in this seed

| Artifact | Where |
|----------|--------|
| Taxonomy / KB design plan | `Document/AgentDesign/AppConfigurator-Commands-Knowledge-Plan.md` |
| Human Help (Form Builder `?`) | `Document/KnowledgeBase/UserGuide/DataModelOperation-Help.md` → sync → `AppReact/public/help/` |

```powershell
.\Document\KnowledgeBase\SyncRuntimeDocs.ps1
```

Syncs **User Help only**. Agent KB is this folder — no Document → seed copy.

## Auto-copy when Config Agent opens

`GenericAgentFileBL.EnsureRoot(..., "app-configurator")`:

1. Packaged `AgentStarterSeeds/app-configurator/` → company `AgentStarter/app-configurator/` (PreserveNewest)
2. Company starter → chat `source/` (missing files only)
