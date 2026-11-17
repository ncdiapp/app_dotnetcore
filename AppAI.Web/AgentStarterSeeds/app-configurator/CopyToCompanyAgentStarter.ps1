# Copy app-configurator AgentStarter seed files into a tenant FileRepository.
# Usage:
#   .\CopyToCompanyAgentStarter.ps1 -CompanyId 1
#   .\CopyToCompanyAgentStarter.ps1 -CompanyId 1 -FileRepositoryRoot "C:\path\to\FileRepository"
param(
  [Parameter(Mandatory = $true)][int]$CompanyId,
  [string]$FileRepositoryRoot = ""
)

$ErrorActionPreference = "Stop"
$seedDir = $PSScriptRoot

if (-not $FileRepositoryRoot) {
  # Default: AppAI.Web sibling FileRepository (dev layout)
  $webRoot = Split-Path (Split-Path $seedDir -Parent) -Parent
  $candidates = @(
    (Join-Path $webRoot "FileRepository"),
    (Join-Path (Split-Path $webRoot -Parent) "FileRepository")
  )
  foreach ($c in $candidates) {
    if (Test-Path $c) { $FileRepositoryRoot = $c; break }
  }
  if (-not $FileRepositoryRoot) {
    throw "FileRepository root not found. Pass -FileRepositoryRoot."
  }
}

$dest = Join-Path $FileRepositoryRoot "Company_$CompanyId\AgentStarter\app-configurator"
New-Item -ItemType Directory -Force -Path $dest | Out-Null

$files = @(
  "commands-knowledge.md",
  "commands-taxonomy-plan.md",
  ".agent-file-catalog.json"
)
foreach ($f in $files) {
  $src = Join-Path $seedDir $f
  if (-not (Test-Path $src)) { throw "Missing seed file: $src" }
  Copy-Item $src (Join-Path $dest $f) -Force
  Write-Host "Copied $f -> $dest"
}

Write-Host "Done. New Configurator chats will pick these up via CopyDefaultSourceToChat."
