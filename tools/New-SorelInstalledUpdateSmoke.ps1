param([string]$OutputRoot = 'artifacts/stable-update-builds', [string]$FeedDirectory = '')
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Run only on a disposable GitHub Actions Windows runner.' }
# Use the actual published baseline, not current sources relabelled as 1.0.0.
$baseline = Join-Path $repo "$OutputRoot/baseline/Sorel.App-stable-Setup.exe"
New-Item -ItemType Directory -Path (Split-Path $baseline) -Force | Out-Null
Invoke-WebRequest 'https://github.com/ZOONGG/Sorel/releases/download/v1.0.0/Sorel.App-stable-Setup.exe' -OutFile $baseline
if ((Get-FileHash -LiteralPath $baseline -Algorithm SHA256).Hash -ne '94D8E2D1FA1E9BA6D16ACE14C8CF522DEEE13E63889C5657E41C755E8118D500') {
    throw 'Published Sorel 1.0.0 setup hash mismatch.'
}
if (!$FeedDirectory) {
    $vpk = Join-Path $repo 'artifacts/velopack-tools/vpk.exe'
    if (!(Test-Path $vpk)) {
        dotnet tool install vpk --version 1.2.161 --tool-path (Split-Path $vpk)
        if ($LASTEXITCODE) { throw 'vpk install failed' }
    }
    & "$PSScriptRoot/New-SorelRelease.ps1" -Version '1.0.1' -OutputRoot "$OutputRoot/1.0.1" -Velopack -VpkPath $vpk -SkipTests
    $FeedDirectory = "$repo/$OutputRoot/1.0.1/velopack"
}
& "$PSScriptRoot/Test-SorelReleaseUpdate.ps1" -SetupPath $baseline -FeedDirectory $FeedDirectory
