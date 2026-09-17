# Patches plm-integration-orchestrator SystemPrompt: Opening behavior -> session_start + ask_user.
# Usage:
#   .\Patch-PlmIntegrationOrchestratorPrompt.ps1 -ServerInstance 'PC3B\MSSQLSERVER01' -Database 'TenantDB_PLM32'
param(
    [Parameter(Mandatory = $true)][string]$ServerInstance,
    [Parameter(Mandatory = $true)][string]$Database,
    [string]$User,
    [string]$Password
)

$ErrorActionPreference = 'Stop'
if ($User) {
    $cs = "Server=$ServerInstance;Database=$Database;User ID=$User;Password=$Password;Encrypt=False;TrustServerCertificate=True;"
} else {
    $cs = "Server=$ServerInstance;Database=$Database;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;"
}

$newOpening = @'
## Session start + ask_user (mandatory)
Empty chat sends a hidden user message `[session_start]` (not shown in UI). Treat that as: **you speak first**.

On `[session_start]` (or any first turn without clear IDs), do **not** call any child yet.
Use the `ask_user` tool for all Gate-0 / menu questions (Interactive HITL). Prefer structured modes over plain chat:

1. **DataSourceIds** — call `ask_user` with:
   - mode=`text`
   - fieldsJson=`[{"name":"plmDataSourceId","label":"PLM DB DataSourceId","required":true},{"name":"dwDataSourceId","label":"PLM Data Warehouse DataSourceId","required":true}]`
   - prompt explaining PLM DB vs DW DB
   - optional contextKey=`plm.integration.job`
   Optional before ask: `list_entity_data_sources` / `explore_platform` to list Id + name, then still `ask_user`.

2. After answers: ensure `write_shared_context` key `plm.integration.job` has
   `{ "plmDataSourceId": <int>, "dwDataSourceId": <int>, "status":"datasources-set" }`
   (ask_user may already merge via contextKey — still verify). Smoke-check connectivity if possible; on failure re-ask with `ask_user`, do not continue.

3. **What to do next** — `ask_user` mode=`single_choice`, optionsJson like:
   `[{"id":"import-dw","label":"1. Import Template TAB from Data Warehouse → Transaction / Form"},{"id":"import-search-view","label":"2. Import Search & View"},{"id":"import-entity","label":"3. Import Entity"}]`

### If they choose **1** (Import Template TAB → Transaction/Form)
Explain briefly: templates are imported **one TemplateId at a time**.
Call `ask_user` mode=`text` for TemplateId (+ APP tenant DataSourceId if unknown as `appDataSourceId`).
Store overrides (e.g. table prefix `Plm_`) into `plm.integration.job` / `plm.integration.import-dw.inputs`.

Then continue the import-dw flow (Phase A → confirmation via `ask_user` or `propose_plan`, then `call_agent`).

Do **not** invent OpeningMessage / static welcome text — always use PROMPT + `[session_start]` + `ask_user`.
'@

$cn = New-Object System.Data.SqlClient.SqlConnection $cs
$cn.Open()
try {
    $cmd = $cn.CreateCommand()
    $cmd.CommandText = "SELECT SystemPrompt FROM dbo.AppAgentSkillSet WHERE SkillKey=N'plm-integration-orchestrator'"
    $old = [string]$cmd.ExecuteScalar()
    if ([string]::IsNullOrEmpty($old)) { throw "SkillKey plm-integration-orchestrator not found in $Database" }

    if ($old -match 'Session start \+ ask_user') {
        Write-Host "Prompt already patched; skipping replace."
    } else {
        $markerStart = '## Opening behavior (mandatory)'
        $idx = $old.IndexOf($markerStart)
        if ($idx -lt 0) {
            # Prepend if old opening section missing
            $updated = $newOpening.TrimEnd() + "`r`n`r`n" + $old
        } else {
            $after = $old.Substring($idx + $markerStart.Length)
            $m = [regex]::Match($after, '(?m)^## (?!#)')
            if (-not $m.Success) { throw "Next ## section not found after Opening behavior" }
            $endIdx = $idx + $markerStart.Length + $m.Index
            $updated = $old.Substring(0, $idx) + $newOpening.TrimEnd() + "`r`n`r`n" + $old.Substring($endIdx)
        }
        $cmd.Parameters.Clear()
        $cmd.CommandText = "UPDATE dbo.AppAgentSkillSet SET SystemPrompt=@p WHERE SkillKey=N'plm-integration-orchestrator'"
        [void]$cmd.Parameters.AddWithValue('@p', $updated)
        $n = $cmd.ExecuteNonQuery()
        Write-Host "Updated rows=$n PromptLen=$($updated.Length)"
    }

    # Idempotent: drop draft OpeningMessage if present; ensure ask_user (same as V030)
    $cmd.Parameters.Clear()
    $cmd.CommandText = @"
IF COL_LENGTH('dbo.AppAgentSkillSet', 'OpeningMessage') IS NOT NULL
    ALTER TABLE dbo.AppAgentSkillSet DROP COLUMN OpeningMessage;
IF NOT EXISTS (SELECT 1 FROM dbo.AppAgentLibraryTool WHERE LibraryKey = N'platform-multi-agent' AND ToolName = N'ask_user')
INSERT INTO dbo.AppAgentLibraryTool
    (LibraryKey, ToolName, ToolDescription, ParameterSchemaJson, ToolType, ToolConfig, IsActive, SortOrder)
VALUES (
    N'platform-multi-agent', N'ask_user',
    N'Ask the user a structured question and wait for their answer (Interactive only).',
    N'{"type":"object","properties":{"prompt":{"type":"string"},"mode":{"type":"string"},"fieldsJson":{"type":"string"},"optionsJson":{"type":"string"},"contextKey":{"type":"string"}},"required":["prompt"]}',
    N'BuiltIn',
    N'{"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.AgentAskUserPlugin","MethodName":"AskUser"}',
    1, 40);
"@
    [void]$cmd.ExecuteNonQuery()
    Write-Host "Schema/tool seed OK for $Database"
}
finally { $cn.Close() }
