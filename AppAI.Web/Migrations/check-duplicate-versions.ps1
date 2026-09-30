# Fails (exit 1) when two migration scripts share a version number, except the known historical duplicates below.
# Run from anywhere: ./check-duplicate-versions.ps1
$knownDuplicates = @(18, 36, 38)   # already deployed — names are tracked by full file name, do not rename

$dupes = Get-ChildItem -Path $PSScriptRoot -Filter 'V*.sql' |
    Where-Object { $_.BaseName -match '^V(\d+)' } |
    Group-Object { [int]($_.BaseName -replace '^V(\d+).*', '$1') } |
    Where-Object { $_.Count -gt 1 -and ($knownDuplicates -notcontains [int]$_.Name) }

if ($dupes) {
    foreach ($d in $dupes) {
        Write-Host ("Duplicate migration number V{0:000}: {1}" -f [int]$d.Name, (($d.Group | ForEach-Object Name) -join ', ')) -ForegroundColor Red
    }
    exit 1
}
Write-Host 'Migration numbers OK.' -ForegroundColor Green
