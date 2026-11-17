# Sync canonical KnowledgeBase manuals → runtime publish copies.
# Does not change business logic — copy only.
#
# Usage (repo root or any cwd):
#   .\Document\KnowledgeBase\SyncRuntimeDocs.ps1
#
# Copies:
#   UserGuide/DataModelOperation-Help.md
#     → AppReact/public/help/commands-user-help.md
#   AgentKnowledge/app-configurator/commands-knowledge.md
#     → AppAI.Web/AgentStarterSeeds/app-configurator/commands-knowledge.md
#   ../AgentDesign/AppConfigurator-Commands-Knowledge-Plan.md
#     → AppAI.Web/AgentStarterSeeds/app-configurator/commands-taxonomy-plan.md
#
# Config Agent auto-copy (unchanged): on EnsureRoot / open chat,
#   AgentStarterSeeds/{skillKey} → FileRepository/.../AgentStarter/{skillKey}
#   → chat source/ (PreserveNewest / missing-only). Keep seeds updated via this script.

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

Copy-Tracked `
  (Join-Path $kbRoot "AgentKnowledge\app-configurator\commands-knowledge.md") `
  (Join-Path $repoRoot "AppAI.Web\AgentStarterSeeds\app-configurator\commands-knowledge.md")

Copy-Tracked `
  (Join-Path $docRoot "AgentDesign\AppConfigurator-Commands-Knowledge-Plan.md") `
  (Join-Path $repoRoot "AppAI.Web\AgentStarterSeeds\app-configurator\commands-taxonomy-plan.md")

Write-Host ""
Write-Host "Done. Rebuild/redeploy AppReact + AppAI.Web so Help UI and AgentStarterSeeds pick up changes."
Write-Host "Config Agent EnsureRoot will then sync AgentStarterSeeds → company AgentStarter → chat source/."
