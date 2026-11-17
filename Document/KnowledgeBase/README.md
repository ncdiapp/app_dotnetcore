# KnowledgeBase — paired runtime manuals

Canonical product manuals that ship into Help UI and AgentStarter seeds.

| Folder | Audience | Runtime destination |
|--------|----------|---------------------|
| `UserGuide/` | Humans (Form Builder Help `?`) | `AppReact/public/help/` |
| `AgentKnowledge/{skillKey}/` | Agents + developers | `AppAI.Web/AgentStarterSeeds/{skillKey}/` |

Design / architecture plans stay in `Document/AgentDesign/` (outside this tree).

## Sync → packaged seeds

```powershell
.\Document\KnowledgeBase\SyncRuntimeDocs.ps1
```

## Config Agent auto-copy (runtime, no Document path)

Opening Configurator (`app-configurator`) calls `GenericAgentFileBL.EnsureRoot` →:

1. `AgentStarterSeeds/app-configurator/` (packaged with web app)  
2. → `FileRepository/Company_{id}/AgentStarter/app-configurator/` (PreserveNewest)  
3. → chat `source/` (missing files only)

Edit under `KnowledgeBase/`, run the sync script, rebuild/redeploy web — tenants pick up new seed files without a manual copy.
