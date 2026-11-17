# AgentStarter seed — `app-configurator`

Copy these files into each tenant's:

`FileRepository/Company_{id}/AgentStarter/app-configurator/`

so new Configurator chats receive them under chat `source/` (`GenericAgentFileBL.CopyDefaultSourceToChat`).

| File | Purpose |
|------|---------|
| `commands-knowledge.md` | **Agent** KB (ActionType ids, storage, tools) — from `Document/AgentDesign/AppConfigurator-Commands-AgentKnowledge.md` |
| `commands-taxonomy-plan.md` | Taxonomy / UI plan (design) |
| `.agent-file-catalog.json` | Short descriptions for file catalog |

**User Help** (in-app `?`) is separate: `Document/AgentDesign/AppConfigurator-Commands-UserHelp.md` → `AppReact/public/help/commands-user-help.md`. Do **not** put the user Help into AgentStarter unless you intentionally want the agent to mirror user wording only.

Canonical authoring copies live under `Document/AgentDesign/`. Re-copy after KB updates.

```powershell
# Dev tenant example
.\CopyToCompanyAgentStarter.ps1 -CompanyId 1
```

Also run migration **V085** so `upsert_command` ActionType is clarified and SystemPrompt indexes `commands-knowledge.md` (prompt append is idempotent).
