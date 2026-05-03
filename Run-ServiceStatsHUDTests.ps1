param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$testProjectPath = Join-Path $PSScriptRoot "ServiceStatsHUD.Tests\ServiceStatsHUD.Tests.csproj"
$testAssemblyPath = Join-Path $PSScriptRoot ("ServiceStatsHUD.Tests\bin\{0}\ServiceStatsHUD.Tests.dll" -f $Configuration)

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
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    )

    foreach ($candidate in $fallbacks) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "MSBuild was not found."
}

function Get-VsTestPath {
    $candidates = @(
        "C:\BuildTools2022\Common7\IDE\Extensions\TestPlatform\vstest.console.exe",
        "C:\BuildTools2022\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "vstest.console.exe was not found."
}

$msbuild = Get-MSBuildPath
$vstest = Get-VsTestPath

Write-Host "Using MSBuild: $msbuild"
Write-Host "Using VSTest: $vstest"

& $msbuild $testProjectPath /restore /t:Build /p:Configuration=$Configuration /p:Platform=x64 /nologo
if ($LASTEXITCODE -ne 0) {
    throw "Test build failed with code $LASTEXITCODE"
}

& $vstest $testAssemblyPath
if ($LASTEXITCODE -ne 0) {
    throw "Tests failed with code $LASTEXITCODE"
}
