# KnowledgeBase — human Help manuals (+ agent KB pointer)

| Folder | Audience | Role |
|--------|----------|------|
| `UserGuide/` | Humans (Form Builder Help `?`) | Canonical → sync to `AppReact/public/help/` |
| `AgentKnowledge/` | Developers | **Pointer only** — real agent KB is `AppAI.Web/AgentStarterSeeds/{skillKey}/` |

Design / architecture plans stay in `Document/AgentDesign/` (outside this tree). Do not put agent runtime KB bodies under `Document/` — LIVE installs may omit or delete `Document/`.

## Sync

```powershell
.\Document\KnowledgeBase\SyncRuntimeDocs.ps1
```

- Copies User Help → `AppReact/public/help/`
- Verifies `AgentStarterSeeds/app-configurator/commands-knowledge.md` exists
- Fails if a design-plan file is left under `AgentStarterSeeds`

## Config Agent auto-copy (runtime)

Opening Configurator (`app-configurator`) / listing Agent Files calls `GenericAgentFileBL.EnsureRoot` / `EnsureStarterRoot` →:

1. `AgentStarterSeeds/app-configurator/` (packaged with web app)  
2. → `FileRepository/Company_{id}/AgentStarter/app-configurator/` (PreserveNewest)  
3. → chat `source/` (missing files only)
