$ErrorActionPreference = 'Stop'
$root = 'c:\DevAppAI\AppAIClean\AppReact\ImportDoc\ImportFromPLMDW\output'
Get-ChildItem $root -Directory | ForEach-Object {
    $f = Join-Path $_.FullName '4_PlmDw_ImportBlueprint.json'
    if (-not (Test-Path $f)) { return }
    $json = Get-Content $f -Raw
    $marker = '"bomColorwayPivotBindings"'
    $idx = $json.IndexOf($marker)
    if ($idx -lt 0) { Write-Host "No key: $($_.Name)"; return }
    $colonIdx = $json.IndexOf(':', $idx + $marker.Length)
    $pos = $colonIdx + 1
    while ($pos -lt $json.Length -and [char]::IsWhiteSpace($json[$pos])) { $pos++ }
    $rest = $json.Substring($pos)
    if ($rest -match '^(null|\{\s*\}|\[\s*\]|\[\s*\{\s*\}\s*\])') {
        $end = $pos + $Matches[0].Length
        $fixed = $json.Substring(0, $pos) + '[]' + $json.Substring($end)
        Set-Content -Path $f -Value $fixed -Encoding UTF8 -NoNewline
        # Ensure trailing newline
        Add-Content -Path $f -Value ''
        Write-Host "Fixed empty bindings: $($_.Name)  (was: $($Matches[0].Trim()))"
    }
    else {
        $preview = $rest.Substring(0, [Math]::Min(40, $rest.Length)).Replace("`r",'').Replace("`n",' ')
        Write-Host "OK: $($_.Name) -> $preview..."
    }
}
