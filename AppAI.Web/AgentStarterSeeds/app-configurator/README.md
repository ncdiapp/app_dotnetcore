# AgentStarter seed — `app-configurator`

Packaged with the web app (`AppAI.Web.csproj` Content → output). On Ensure Form / Search / AppBuilder chat (and EnsureRoot), files sync **PreserveNewest** into:

`FileRepository/Company_{id}/AgentStarter/app-configurator/`

Chat `source/` uses that company folder. Tenants do **not** need a manual seed copy after deploy; rebuild/redeploy web is enough.

## Files

| File | Role |
|------|------|
| `commands-knowledge.md` | Agent deep manual (UI + actionType / upsert) |
| `commands-taxonomy-plan.md` | Taxonomy / picker / KB plan (design mirror) |
| `.agent-file-catalog.json` | Optional catalog for agent file listing |

## Canonical sources (edit these, then sync)

| Seed file | Canonical |
|-----------|-----------|
| `commands-knowledge.md` | `Document/KnowledgeBase/AgentKnowledge/app-configurator/commands-knowledge.md` |
| `commands-taxonomy-plan.md` | `Document/AgentDesign/AppConfigurator-Commands-Knowledge-Plan.md` |

```powershell
.\Document\KnowledgeBase\SyncRuntimeDocs.ps1
```

User Help (in-app `?`) is separate:  
canonical `Document/KnowledgeBase/UserGuide/DataModelOperation-Help.md` → `AppReact/public/help/commands-user-help.md` (same sync script).

## Auto-copy when Config Agent opens

`GenericAgentFileBL.EnsureRoot(..., "app-configurator")`:

1. Packaged `AgentStarterSeeds/app-configurator/` → company `AgentStarter/app-configurator/` (PreserveNewest)
2. Company starter → chat `source/` (missing files only)

This folder path is the runtime source of truth for step 1 — keep it updated via the sync script above.
