$source = Join-Path $PSScriptRoot "workshop"
$target = "C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\Mods\ServiceStatsHUD"

if (-not (Test-Path $source)) {
    throw "Source folder not found: $source"
}

New-Item -ItemType Directory -Force -Path $target | Out-Null
Copy-Item -Path (Join-Path $source "*") -Destination $target -Recurse -Force

Write-Host "Synced workshop folder to: $target"
