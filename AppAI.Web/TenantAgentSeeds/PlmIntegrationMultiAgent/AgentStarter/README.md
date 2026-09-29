# Default Source files for PLM Multi-Agent seed pack

These are **tenant seed assets** for this agent configuration only. They are **not** copied by the web build and **not** seeded from APP.BL.

## Layout

```
AgentStarter/
  _packs/
    search/       → plm-integration-search + plm-integration-orchestrator
    massupdate/   → plm-integration-massupdate + plm-integration-orchestrator
    import-dw/    → plm-integration-import-dw + plm-integration-orchestrator
```

Each pack folder holds probe scripts, example JSON, optional `.agent-file-catalog.json`, and generators.

## Apply (with RUN_ALL)

```bat
RUN_ALL.bat Server Instance TenantDb CompanyId
```

Example:

```bat
RUN_ALL.bat PC3B\MSSQLSERVER01 TenantDB_PLM34 1
```

Optional 4th argument: full path to `FileRepository` (parent of `Company_{id}`). Default: `AppAI.Web\bin\Debug\net10.0\FileRepository`.

After SQL seeds, `CopyAgentStarter.ps1` copies `_packs/*` into  
`FileRepository/Company_{CompanyId}/AgentStarter/{skillKey}/`.

New Chat copies from AgentStarter into chat `source/` via the product UI (Restore / first session copy).
