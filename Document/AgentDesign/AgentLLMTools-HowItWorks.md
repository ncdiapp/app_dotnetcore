# How Agent, LLM, and Tools Work Together

## Overview

An agent is a domain specialist built from three components:

| Component | Role |
|---|---|
| **System Prompt** | Domain knowledge + behavior rules — turns a generic LLM into a specialist |
| **LLM** | Reasoning engine — reads the conversation, decides which tool to call and with what args |
| **Tools** | Capabilities — the actions the agent can actually take |

---

## The Three-Part Request

Every agent run has three inputs the LLM sees simultaneously:

```
SYSTEM PROMPT  →  "Who you are, what you know, how to behave"
USER MESSAGE   →  "What the user wants done right now"
TOOLS LIST     →  "What actions are available to you"
```

Example for `"check if applistmenu exist"`:

```
SYSTEM: You are AppBuilder AI embedded in the AppAI platform.
        AppListMenu is the navigation menu table...
        Only use SELECT queries, never DROP or DELETE...

USER:   check if applistmenu exist

TOOLS:  execute_sql    — Execute a SQL SELECT query
        check_table_exists — Check whether a table exists
        create_app_package — ...
```

The LLM reads all three and decides: call `execute_sql` with
`SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AppListMenu'`.

---

## How a Tool Gets Called — Full Flow

```
POST /webapi/GenericAgent/RunAgent  (SkillKey + UserMessage)
  │
  ├─ Load agent config         AppAgentSkillSetBL.GetByKey(skillKey)
  │    SystemPrompt, MaxIterations, CapabilityFlags, ExecutionMode
  │
  ├─ Load tools from DB        AppAgentToolRegisterBL.GetBySkillKey(skillKey)
  │    SELECT * FROM dbo.AppAgentToolRegister WHERE SkillKey=@key AND IsActive=1
  │    Each row: ToolName | Description | ParameterSchemaJson | ToolType | ToolConfig
  │
  ├─ Wrap each tool             GenericAgentEngine.WrapRegisteredTool()
  │    KernelFunctionFactory.CreateFromMethod(
  │        functionName: "execute_sql",
  │        description:  "Execute a SQL SELECT query...",   ← what LLM reads
  │        parameters:   parsed from ParameterSchemaJson    ← what LLM fills in
  │    )
  │
  ├─ Send to LLM                ChatCompletionAgent.InvokeStreamingAsync()
  │    SK serializes all KernelFunctions → Anthropic "tools" array
  │    LLM picks the right tool based on Description
  │
  ├─ LLM returns tool_use
  │    { "name": "execute_sql", "input": { "sql": "SELECT ..." } }
  │
  ├─ Route by ToolType          AppAgentToolEngine.Dispatch()
  │    "BuiltIn"       → BuiltInToolExecutor
  │    "SqlQuery"      → SqlQueryToolExecutor
  │    "HttpRest"      → HttpRestToolExecutor
  │    "PowerShell"    → PowerShellToolExecutor
  │    "ExternalDll"   → ExternalDllToolExecutor
  │
  ├─ Execute (BuiltIn example)  BuiltInToolExecutor.ExecuteAsync()
  │    1. Parse ToolConfig → TypeName + MethodName
  │    2. Resolve via reflection (cached process-lifetime)
  │    3. Create plugin instance (cached per agent run)
  │    4. Restore tenant identity on worker thread
  │    5. BuildParams() — map LLM args + auto-inject by type:
  │         args["sql"]         → matched by parameter name
  │         CancellationToken   → injected automatically
  │         AgentToolContext    → injected automatically
  │    6. method.Invoke(instance, params)
  │
  └─ Result → back to LLM → next turn → final text response → SSE stream to client
```

---

## Parameter Sources (BuiltIn tools)

ToolConfig has **no** parameter info — parameters come from two separate sources:

| Source | Where configured | Purpose |
|---|---|---|
| `ParameterSchemaJson` (DB field) | Tool Register UI | Tells the **LLM** what args to fill in |
| `BuildParams()` auto-injection | Code | Injects `CancellationToken`, `AgentToolContext`, `AppClientIdentity` automatically |

Example — `DataQueryPlugin.ExecuteSql(string sql)`:
- `sql` → matched from LLM args by parameter name, converted to `string`
- No `AgentToolContext` parameter here, so nothing auto-injected

---

## Why System Prompt Matters

The **Description** field on each tool tells the LLM *what the tool does*.  
The **System Prompt** tells the LLM *when and how to use it*.

Without System Prompt → generic LLM, no platform knowledge, wrong decisions.  
With System Prompt → domain specialist with rules, naming conventions, and judgment.

### System Prompt's three jobs

| Job | Example |
|---|---|
| **Identity** | "You are AppBuilder AI, your job is to build applications..." |
| **Domain knowledge** | Table names, platform concepts, entity relationships, business rules |
| **Behavior rules** | "Only SELECT, never DROP", "Propose a plan before executing", "Use check_table_exists before creating" |

---

## Each Agent = A Domain Specialist

| Agent Code | Domain | Specialist in |
|---|---|---|
| `app-builder` | App construction | Form/menu/workflow/schema build sequence |
| `db-genie` | Database | Schema reading, SELECT queries, entity structures |
| `app-report` | Reporting | Report layout, data source wiring, field binding |
| `data-integ` | Integration | API connectors, mapping rules, external data |

The same LLM engine powers all agents — what makes each specialist is the **System Prompt** (domain knowledge) and the **active tool set** (scoped capabilities).

---

## Key Files

| Layer | File |
|---|---|
| HTTP entry + SSE streaming | `AppAI.Web/Controllers/GenericAgentController.cs` |
| Agentic loop + tool wrapping | `APP.BL/AIAgent/GenericAgent/GenericAgentEngine.cs` |
| Tool type router | `APP.BL/TenantBusiness/AppAgentToolEngine.cs` |
| BuiltIn reflection executor | `APP.BL/TenantBusiness/AgentToolExecutors/BuiltInToolExecutor.cs` |
| Example plugin | `APP.BL/AIAgent/AppBuilderAgent/Plugins/DataQueryPlugin.cs` |
| Agent config DB access | `APP.BL/TenantBusiness/AppAgentSkillSetBL.cs` |
| Tool register DB access | `APP.BL/TenantBusiness/AppAgentToolRegisterBL.cs` |
