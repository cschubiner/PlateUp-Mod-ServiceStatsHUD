param(
    [string]$PlateUpInstallDir = 'C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp',
    [string]$PreviewPath,
    [switch]$DependenciesOnly
)

$ErrorActionPreference = 'Stop'
$metadata = Get-Content (Join-Path $PSScriptRoot 'workshop\plateup_mod_metadata.json') -Raw | ConvertFrom-Json
$itemId = [uint64]$metadata.steamWorkshopItemID
if ($itemId -ne 3799437092) { throw 'Unexpected Workshop item ID; refusing to update another item.' }

$assemblyPath = Join-Path $PlateUpInstallDir 'PlateUp_Data\Managed\Facepunch.Steamworks.Win64.dll'
$nativePath = Join-Path $PlateUpInstallDir 'PlateUp_Data\Plugins\x86_64'
$env:PATH = "$nativePath;$env:PATH"
$assembly = [Reflection.Assembly]::LoadFrom($assemblyPath)

function Wait-WorkshopTask($Task) {
    $timer = [Diagnostics.Stopwatch]::StartNew()
    while (-not $Task.IsCompleted) {
        if ($timer.Elapsed.TotalSeconds -gt 90) { throw 'Steam request timed out. Check the existing item before retrying.' }
        Start-Sleep -Milliseconds 100
    }
    return $Task.GetAwaiter().GetResult()
}

try {
    [Steamworks.SteamClient]::Init(1599600, $true)
    $publishedId = [Steamworks.Data.PublishedFileId]$itemId
    $item = Wait-WorkshopTask ([Steamworks.Ugc.Item]::GetAsync($publishedId, 0))
    if (-not $item -or $item.Title -ne 'Service Stats HUD') { throw 'Could not verify Service Stats HUD on Steam.' }

    # This game-bundled wrapper exposes dependency updates on its internal UGC interface.
    $flags = [Reflection.BindingFlags]'Static,NonPublic'
    $ugc = $assembly.GetType('Steamworks.SteamUGC').GetProperty('Internal', $flags).GetValue($null)
    $method = $ugc.GetType().GetMethod('AddDependency', [Reflection.BindingFlags]'Instance,NonPublic,Public')
    foreach ($dependency in [uint64[]]@(2898033283, 2898069883, 2949018507)) {
        $call = $method.Invoke($ugc, @($publishedId, [Steamworks.Data.PublishedFileId]$dependency))
        $type = $call.GetType()
        $timer = [Diagnostics.Stopwatch]::StartNew()
        while (-not $type.GetProperty('IsCompleted').GetValue($call)) {
            if ($timer.Elapsed.TotalSeconds -gt 45) { throw "Dependency request timed out: $dependency" }
            Start-Sleep -Milliseconds 100
        }
        $dependencyResult = $type.GetMethod('GetResult').Invoke($call, @())
        $status = if ($dependencyResult) {
            $dependencyResult.GetType().GetField('Result', [Reflection.BindingFlags]'Instance,Public,NonPublic').GetValue($dependencyResult)
        } else { $null }
        if (-not $status -or $status.ToString() -notin @('OK', 'DuplicateRequest')) {
            throw "Failed to add dependency ${dependency}: $status"
        }
        Write-Host "Required item: $dependency"
    }
    if (-not $DependenciesOnly) {
        $editor = $item.Edit().WithPublicVisibility()
        if ($PreviewPath) {
            $preview = (Resolve-Path -LiteralPath $PreviewPath).Path
            $editor = $editor.WithPreviewFile($preview)
        }
        $result = Wait-WorkshopTask ($editor.SubmitAsync($null))
        if ($result.Result.ToString() -ne 'OK') { throw "Steam update failed: $($result.Result). Check the listing for pending content review or account requirements." }
        if ($result.NeedsWorkshopAgreement) { throw 'The Steam account must accept the Workshop agreement.' }
        Write-Host "Updated Workshop item $itemId."
    }
    $verified = Wait-WorkshopTask ([Steamworks.Ugc.Item]::GetAsync($publishedId, 0))
    Write-Host "Public: $($verified.IsPublic)"
    Write-Host $verified.Url
}
finally {
    [Steamworks.SteamClient]::Shutdown()
}
