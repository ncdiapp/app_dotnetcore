# AgentKnowledge — pointer only (not the edit source)

Runtime agent KB bodies **do not live here**. Edit and ship them from:

```
AppAI.Web/AgentStarterSeeds/{skillKey}/
```

That folder is packaged with the web app, appears in Agent Management → **Files** (Default Source), and syncs to company `AgentStarter/{skillKey}/` → chat `source/`.

| skillKey | Canonical runtime KB |
|----------|----------------------|
| `app-configurator` | `AppAI.Web/AgentStarterSeeds/app-configurator/commands-knowledge.md` |

Design / taxonomy plans: `Document/AgentDesign/` (never under `AgentStarterSeeds`).  
Human Help: `Document/KnowledgeBase/UserGuide/` → `SyncRuntimeDocs.ps1` → `AppReact/public/help/`.

```powershell
.\Document\KnowledgeBase\SyncRuntimeDocs.ps1
```

Syncs User Help only; verifies agent seed KB exists and no design-plan file is under `AgentStarterSeeds`.
