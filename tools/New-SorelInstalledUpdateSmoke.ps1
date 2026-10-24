param([string]$OutputRoot = 'artifacts/stable-update-builds')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$vpk = Join-Path $repo 'artifacts/velopack-tools/vpk.exe'
if (!(Test-Path $vpk)) {
    dotnet tool install vpk --version 1.2.161 --tool-path (Split-Path $vpk)
    if ($LASTEXITCODE) { throw 'vpk install failed' }
}
foreach ($version in '1.0.0','1.0.1') {
    & "$PSScriptRoot/New-SorelRelease.ps1" -Version $version -OutputRoot "$OutputRoot/$version" -Velopack -VpkPath $vpk -SkipTests
}
& "$PSScriptRoot/Test-SorelReleaseUpdate.ps1" -SetupPath "$repo/$OutputRoot/1.0.0/velopack/Sorel.App-stable-Setup.exe" -FeedDirectory "$repo/$OutputRoot/1.0.1/velopack"