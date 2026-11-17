# AgentKnowledge — manuals for agents (machine + developers)

**Canonical** English knowledge bases consumed by Configurator / other agents via AgentStarter chat `source/`.

| Not here | Where |
|----------|--------|
| Human User Help | `Document/KnowledgeBase/UserGuide/` |
| Design plans | `Document/AgentDesign/` |
| Packaged seed (runtime) | `AppAI.Web/AgentStarterSeeds/{skillKey}/` |

## Layout

```
Document/KnowledgeBase/AgentKnowledge/{skillKey}/commands-knowledge.md
```

`{skillKey}` matches Agent code (e.g. `app-configurator`).

## Current packs

| skillKey | Canonical | Packaged seed |
|----------|-----------|-----------------|
| `app-configurator` | `app-configurator/commands-knowledge.md` | `AppAI.Web/AgentStarterSeeds/app-configurator/commands-knowledge.md` |

Sync after edits:

```powershell
.\Document\KnowledgeBase\SyncRuntimeDocs.ps1
```

Web app auto-copies packaged seeds → `FileRepository/Company_{id}/AgentStarter/{skillKey}/` on Ensure (PreserveNewest).
