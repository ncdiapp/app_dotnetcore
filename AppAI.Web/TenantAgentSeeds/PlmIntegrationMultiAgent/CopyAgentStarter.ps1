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
$catalogName = ".agent-file-catalog.json"

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

function Get-StarterDest {
    param([string]$CompanyRoot, [string]$SkillKey)
    Join-Path (Join-Path $CompanyRoot "AgentStarter") $SkillKey
}

function Copy-PackFiles {
    param([string]$SrcDir, [string]$DestDir)
    New-Item -ItemType Directory -Force -Path $DestDir | Out-Null
    Get-ChildItem -LiteralPath $SrcDir -Force | Where-Object { -not $_.PSIsContainer } | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $DestDir -Force
    }
}

function Copy-PackCatalog {
    param([string]$PackDir, [string]$DestDir)
    $catSrc = Join-Path $PackDir $catalogName
    if (Test-Path -LiteralPath $catSrc) {
        Copy-Item -LiteralPath $catSrc -Destination (Join-Path $DestDir $catalogName) -Force
    }
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
        $dest = Get-StarterDest -CompanyRoot $DestCompanyRoot -SkillKey $skillKey
        Copy-PackFiles -SrcDir $src -DestDir $dest
        Copy-PackCatalog -PackDir $src -DestDir $dest
        Write-Host "  $PackName -> AgentStarter/$skillKey"
    }
}

function Write-MergedOrchestratorCatalog {
    param([string]$OrchDest)
    $catalogPaths = @("search", "massupdate", "import-dw") | ForEach-Object {
        Join-Path (Join-Path $packsRoot $_) $catalogName
    } | Where-Object { Test-Path -LiteralPath $_ }

    if ($catalogPaths.Count -eq 0) { return }

    $merged = [ordered]@{ Version = 1; Files = @() }
    $seen = @{}
    foreach ($cp in $catalogPaths) {
        $cat = Get-Content -LiteralPath $cp -Raw | ConvertFrom-Json
        foreach ($f in $cat.Files) {
            if (-not $f.Path -or $seen.ContainsKey($f.Path)) { continue }
            $seen[$f.Path] = $true
            $merged.Files += $f
        }
    }
    $outPath = Join-Path $OrchDest $catalogName
    $json = $merged | ConvertTo-Json -Depth 8
    [System.IO.File]::WriteAllText($outPath, $json, [System.Text.UTF8Encoding]::new($false))
    Write-Host "  merged $catalogName -> plm-integration-orchestrator ($($merged.Files.Count) entries)"
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

$orchDest = Get-StarterDest -CompanyRoot $companyRoot -SkillKey "plm-integration-orchestrator"
if (Test-Path $orchDest) {
    Write-MergedOrchestratorCatalog -OrchDest $orchDest
}

Write-Host "=== AgentStarter copy DONE ==="
