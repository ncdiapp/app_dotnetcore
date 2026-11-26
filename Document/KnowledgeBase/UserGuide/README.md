# UserGuide — runtime product manuals (human)

**Canonical** English manuals for people using the running product (Form Builder, Help `?`, training).

| Not here | Where |
|----------|--------|
| Design / architecture / implementation plans | `Document/AgentDesign/` |
| Agent-facing knowledge (actionType, tools, storage) | `AppAI.Web/AgentStarterSeeds/{skillKey}/` (runtime KB; not under Document) |
| Deployed Help fetch copy | `AppReact/public/help/` (generated) |

## Current manuals

| Canonical | Runtime copy |
|-----------|----------------|
| `DataModelOperation-Help.md` | `AppReact/public/help/commands-user-help.md` |

After editing a canonical file, run from repo root:

```powershell
.\Document\KnowledgeBase\SyncRuntimeDocs.ps1
```

Then rebuild/redeploy React. Agent KB edits go directly under `AppAI.Web/AgentStarterSeeds/` (rebuild/redeploy Web).
