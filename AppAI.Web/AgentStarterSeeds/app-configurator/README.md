# AgentStarter seed — `app-configurator`

Copy these files into each tenant's:

`FileRepository/Company_{id}/AgentStarter/app-configurator/`

so new Configurator chats receive them under chat `source/` (`GenericAgentFileBL.CopyDefaultSourceToChat`).

| File | Purpose |
|------|---------|
| `commands-knowledge.md` | Full Data Model Operation KB for `read_agent_file` |
| `commands-taxonomy-plan.md` | Taxonomy / UI plan |
| `.agent-file-catalog.json` | Short descriptions shown in file catalog / prompt summary |

Canonical authoring copies live under `Document/AgentDesign/`. Re-copy after KB updates.

```powershell
# Dev tenant example
.\CopyToCompanyAgentStarter.ps1 -CompanyId 1
```

Also run migration **V085** (`V085__AppConfigurator_CommandsUpsertAndKnowledgeIndex_xhu.sql`) so `upsert_command` ActionType is clarified and SystemPrompt indexes this KB (prompt append is idempotent).
