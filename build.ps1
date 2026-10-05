<#
.SYNOPSIS
    Builds ChatPlus and packs a Thunderstore / r2modman package into dist\.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File build.ps1
    powershell -ExecutionPolicy Bypass -File build.ps1 -BepInExCore "D:\r2modman\...\BepInEx\core"
#>
param(
    [string]$Configuration = 'Release',
    # Folder that contains OnTogether.exe (default: the game folder next to this project).
    [string]$GameDir,
    # BepInEx\core folder of any profile (default: Thunderstore Mod Manager "Default" profile).
    [string]$BepInExCore
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# A dotnet host that actually has an SDK: PATH first, then the per-user and machine-wide installs.
$dotnet = $null
$candidates = @(
    (Get-Command dotnet -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -First 1),
    "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe",
    "$env:ProgramFiles\dotnet\dotnet.exe"
)
foreach ($candidate in $candidates) {
    if ($candidate -and (Test-Path $candidate) -and (& $candidate --list-sdks 2>$null)) { $dotnet = $candidate; break }
}
if (-not $dotnet) { throw '.NET SDK not found. Install the .NET SDK 6.0 or newer.' }

$buildArgs = @('build', (Join-Path $root 'src\ChatPlus.csproj'), '-c', $Configuration, '-nologo', '-v:minimal')
if ($GameDir) { $buildArgs += "-p:GameDir=$GameDir" }
if ($BepInExCore) { $buildArgs += "-p:BepInExCore=$BepInExCore" }
& $dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

$manifest = Get-Content (Join-Path $root 'package\manifest.json') -Raw | ConvertFrom-Json
$dll = Join-Path $root "src\bin\$Configuration\ChatPlus.dll"
$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($dll).Version
if ("$($assemblyVersion.Major).$($assemblyVersion.Minor).$($assemblyVersion.Build)" -ne $manifest.version_number) {
    throw "Version mismatch: manifest.json has $($manifest.version_number), the assembly has $assemblyVersion (update both, plus Plugin.PluginVersion)."
}

$dist = Join-Path $root 'dist'
$stage = Join-Path $dist 'stage'
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Copy-Item (Join-Path $root 'package\*') $stage
Copy-Item $dll $stage

$zip = Join-Path $dist "$($manifest.name)-$($manifest.version_number).zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
Remove-Item $stage -Recurse -Force
Write-Host "Package: $zip"
