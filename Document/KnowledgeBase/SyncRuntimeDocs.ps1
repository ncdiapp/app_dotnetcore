# Sync canonical User Help manuals → runtime publish copies.
# Agent KB is NOT synced from Document — edit AppAI.Web/AgentStarterSeeds/{skillKey}/ directly.
#
# Usage (repo root or any cwd):
#   .\Document\KnowledgeBase\SyncRuntimeDocs.ps1
#
# Copies:
#   UserGuide/DataModelOperation-Help.md
#     → AppReact/public/help/commands-user-help.md
#
# Verifies (does not copy):
#   AppAI.Web/AgentStarterSeeds/app-configurator/commands-knowledge.md exists
#   Design plan stays only under Document/AgentDesign/ (must NOT be under AgentStarterSeeds)
#
# Config Agent auto-copy (unchanged): on EnsureRoot / open chat / Files list,
#   AgentStarterSeeds/{skillKey} → FileRepository/.../AgentStarter/{skillKey}
#   → chat source/ (PreserveNewest / missing-only).

$ErrorActionPreference = "Stop"
$kbRoot = $PSScriptRoot
$docRoot = Split-Path $kbRoot -Parent
$repoRoot = Split-Path $docRoot -Parent

function Copy-Tracked([string]$Src, [string]$Dest) {
  if (-not (Test-Path $Src)) { throw "Missing source: $Src" }
  $destDir = Split-Path $Dest -Parent
  New-Item -ItemType Directory -Force -Path $destDir | Out-Null
  Copy-Item $Src $Dest -Force
  Write-Host "OK  $Src"
  Write-Host " -> $Dest"
}

Copy-Tracked `
  (Join-Path $kbRoot "UserGuide\DataModelOperation-Help.md") `
  (Join-Path $repoRoot "AppReact\public\help\commands-user-help.md")

$agentKb = Join-Path $repoRoot "AppAI.Web\AgentStarterSeeds\app-configurator\commands-knowledge.md"
if (-not (Test-Path $agentKb)) {
  throw "Missing agent KB (canonical seed): $agentKb"
}
Write-Host "OK  agent KB present (edit in place, not synced from Document):"
Write-Host "    $agentKb"

$staleTaxonomy = Join-Path $repoRoot "AppAI.Web\AgentStarterSeeds\app-configurator\commands-taxonomy-plan.md"
if (Test-Path $staleTaxonomy) {
  throw "Design plan must not live under AgentStarterSeeds. Move/delete: $staleTaxonomy (canonical: Document/AgentDesign/AppConfigurator-Commands-Knowledge-Plan.md)"
}
Write-Host "OK  no design-plan file under AgentStarterSeeds/app-configurator/"

Write-Host ""
Write-Host "Done. Rebuild/redeploy AppReact for Help UI; rebuild/redeploy AppAI.Web for AgentStarterSeeds."
Write-Host "Config Agent EnsureRoot will then sync AgentStarterSeeds → company AgentStarter → chat source/."
