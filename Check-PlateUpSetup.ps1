$plateUpInstallDir = "C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp"
$plateUpWorkshopDir = "C:\Program Files (x86)\Steam\steamapps\workshop\content\1599600"
$plateUpModsDir = Join-Path $plateUpInstallDir "Mods"

$checks = @(
    @{ Name = "PlateUp.exe"; Path = Join-Path $plateUpInstallDir "PlateUp.exe" },
    @{ Name = "ModUploader.exe"; Path = Join-Path $plateUpInstallDir "PlateUp_Data\ModUploader.exe" },
    @{ Name = "HarmonyX"; Path = Join-Path $plateUpWorkshopDir "2898033283\0Harmony.dll" },
    @{ Name = "KitchenLib"; Path = Join-Path $plateUpWorkshopDir "2898069883\KitchenLib-Workshop.dll" },
    @{ Name = "PreferenceSystem"; Path = Join-Path $plateUpWorkshopDir "2949018507\PreferenceSystem-Workshop.dll" }
)

foreach ($check in $checks) {
    $exists = Test-Path $check.Path
    $status = if ($exists) { "OK" } else { "MISSING" }
    Write-Host ("{0,-14} {1}  {2}" -f $check.Name, $status, $check.Path)
}

Write-Host ""
Write-Host ("Local mod target: {0}" -f $plateUpModsDir)
Write-Host ("Workshop staging: {0}" -f (Join-Path $PSScriptRoot "workshop\content"))
