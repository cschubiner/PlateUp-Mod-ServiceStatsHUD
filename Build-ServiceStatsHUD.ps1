param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$projectPath = Join-Path $PSScriptRoot "ServiceStatsHUD.csproj"

function Get-MSBuildPath {
    $vswhere = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $path = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
        if ($path) {
            return $path
        }
    }

    $fallbacks = @(
        "C:\BuildTools2022\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    )

    foreach ($candidate in $fallbacks) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "MSBuild was not found. Install Visual Studio 2022 or Build Tools with the .NET desktop/MSBuild workload."
}

$msbuild = Get-MSBuildPath
Write-Host "Using MSBuild: $msbuild"
Write-Host "Building configuration: $Configuration"

& $msbuild $projectPath /restore /t:Build /p:Configuration=$Configuration /p:Platform=x64
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild exited with code $LASTEXITCODE"
}
