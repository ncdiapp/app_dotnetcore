# Copies Default Source files from this seed pack into FileRepository AgentStarter folders.
# Not part of APP.BL — run from RUN_ALL.bat after SQL seeds (optional).
param(
    [Parameter(Mandatory = $true)]
    [int]$CompanyId,
    [string]$FileRepositoryRoot = ""
)

$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$packsRoot = Join-Path $here "AgentStarter\_packs"

if (-not (Test-Path $packsRoot)) {
    Write-Error "Missing AgentStarter\_packs under $here"
}

function Resolve-FileRepositoryRoot {
    param([string]$Override)
    if ($Override -and (Test-Path $Override)) { return (Resolve-Path $Override).Path }

    $candidates = @(
        (Join-Path $here "..\..\bin\Debug\net10.0\FileRepository"),
        (Join-Path $here "..\..\bin\Release\net10.0\FileRepository"),
        (Join-Path $here "..\bin\Debug\net10.0\FileRepository")
    )
    foreach ($c in $candidates) {
        try {
            $full = [System.IO.Path]::GetFullPath($c)
            if (Test-Path (Split-Path $full -Parent)) {
                New-Item -ItemType Directory -Force -Path $full | Out-Null
                return $full
            }
        } catch { }
    }
    throw "FileRepository not found. Pass -FileRepositoryRoot (folder that contains Company_{id})."
}

function Copy-PackToStarter {
    param(
        [string]$PackName,
        [string[]]$SkillKeys,
        [string]$DestCompanyRoot
    )
    $src = Join-Path $packsRoot $PackName
    if (-not (Test-Path $src)) {
        Write-Warning "Skip pack '$PackName' (folder not found)."
        return
    }
    foreach ($skillKey in $SkillKeys) {
        $dest = Join-Path $DestCompanyRoot "AgentStarter" $skillKey
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        Copy-Item -Path (Join-Path $src "*") -Destination $dest -Recurse -Force
        Write-Host "  $PackName -> AgentStarter/$skillKey"
    }
}

$repoRoot = Resolve-FileRepositoryRoot -Override $FileRepositoryRoot
$companyRoot = Join-Path $repoRoot "Company_$CompanyId"
New-Item -ItemType Directory -Force -Path $companyRoot | Out-Null

Write-Host "=== Copy AgentStarter (CompanyId=$CompanyId) ==="
Write-Host "FileRepository: $repoRoot"

Copy-PackToStarter -PackName "search" -SkillKeys @(
    "plm-integration-search",
    "plm-integration-orchestrator"
) -DestCompanyRoot $companyRoot

Copy-PackToStarter -PackName "massupdate" -SkillKeys @(
    "plm-integration-massupdate",
    "plm-integration-orchestrator"
) -DestCompanyRoot $companyRoot

Copy-PackToStarter -PackName "import-dw" -SkillKeys @(
    "plm-integration-import-dw",
    "plm-integration-orchestrator"
) -DestCompanyRoot $companyRoot

$orchDest = Join-Path $companyRoot "AgentStarter" "plm-integration-orchestrator"
$catalogPaths = @("search", "massupdate", "import-dw") | ForEach-Object {
    Join-Path $packsRoot $_ ".agent-file-catalog.json"
} | Where-Object { Test-Path $_ }

if ((Test-Path $orchDest) -and $catalogPaths.Count -gt 0) {
    $merged = @{ Version = 1; Files = @() }
    $seen = @{}
    foreach ($cp in $catalogPaths) {
        $cat = Get-Content $cp -Raw | ConvertFrom-Json
        foreach ($f in $cat.Files) {
            if (-not $f.Path -or $seen.ContainsKey($f.Path)) { continue }
            $seen[$f.Path] = $true
            $merged.Files += $f
        }
    }
    $merged | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $orchDest ".agent-file-catalog.json") -Encoding UTF8
    Write-Host "  merged .agent-file-catalog.json -> plm-integration-orchestrator"
}

Write-Host "=== AgentStarter copy DONE ==="
