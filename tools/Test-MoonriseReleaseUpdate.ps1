param(
    [ValidateSet('local','github')][string]$FeedMode = 'github',
    [string]$ReleaseTag = 'v0.1.0-beta.2'
)
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Run only on a disposable GitHub Actions Windows runner.' }
$repo = Split-Path $PSScriptRoot -Parent
$root = Join-Path $repo "artifacts/release-update-$FeedMode"
$install = Join-Path $root 'install'
$data = Join-Path $root 'data'
$helperOutput = Join-Path $root 'helper'
New-Item -ItemType Directory $root,$data,$helperOutput -Force | Out-Null
if (Test-Path 'Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\Moonrise.App') {
    throw 'Production registration exists on runner.'
}
$setup = Join-Path $root 'beta1-setup.exe'
Invoke-WebRequest 'https://github.com/ZOONGG/Moonrise/releases/download/v0.1.0-beta.1/Moonrise.App-Setup.exe' -OutFile $setup
if ((Get-FileHash $setup).Hash -ne '9345AC5FB43D96A18D319315E8F84A4019B604B136DCE32992E5EA5DD4976A82') { throw 'Old Setup hash mismatch.' }
function Run($exe, $arguments, $name, $environment = @{}) {
    $p = Start-Process $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru -Environment $environment `
        -RedirectStandardOutput "$root/$name.stdout.txt" -RedirectStandardError "$root/$name.stderr.txt"
    if (!$p.WaitForExit(180000) -or $p.ExitCode -ne 0) {
        Get-Content "$root/$name.stdout.txt", "$root/$name.stderr.txt" -ErrorAction SilentlyContinue
        throw "$name failed"
    }
}
Run $setup @('--silent','--installto',('"'+$install+'"')) 'install-beta1'
$current = Join-Path $install 'current'
if ((Get-Item "$current/Moonrise.exe").VersionInfo.ProductVersion -ne '0.1.0-beta.1') { throw 'Old installed version mismatch.' }
dotnet publish "$repo/tools/ReleaseUpdateSmoke/ReleaseUpdateSmoke.csproj" -c Release -p:RestoreLockedMode=true -o $helperOutput
if ($LASTEXITCODE) { throw 'Helper build failed.' }
function Copy-Helper {
    Get-ChildItem $helperOutput -File | Where-Object Name -ne 'Moonrise.exe' | ForEach-Object { Copy-Item $_.FullName $current -Force }
}
Copy-Helper
$helper = Join-Path $current 'ReleaseUpdateSmoke.exe'
Run $helper @(('"'+$data+'"'),'seed',('"'+$current+'/runtime/catalog/Veyra-0.1.1-moonrise.1.jar"')) 'seed'
$protected = @(Get-ChildItem "$data/settings", "$data/packages" -Recurse -File | Where-Object { $_.Extension -eq '.jar' -or $_.Name -eq 'moonrise-settings.json' })
$before = @{}; foreach ($file in $protected) { $before[$file.FullName] = (Get-FileHash $file.FullName).Hash }
if ($FeedMode -eq 'local') {
    $feed = Join-Path $root 'feed'; New-Item -ItemType Directory $feed | Out-Null
    gh release download $ReleaseTag -R ZOONGG/Moonrise -p '*full.nupkg' -p 'releases.beta.json' -D $feed
    if ($LASTEXITCODE) { throw 'Draft feed download failed.' }
} else { $feed = 'https://github.com/ZOONGG/Moonrise' }
$screenshot = Join-Path $root 'restarted-beta2.png'
Run $helper @(('"'+$feed+'"'),'apply') 'apply' @{
    MOONRISE_VISUAL_QA='1'; MOONRISE_VISUAL_QA_DATA_ROOT=$data
    MOONRISE_VISUAL_QA_SCREENSHOT=$screenshot; MOONRISE_VISUAL_QA_EXIT_AFTER_SCREENSHOT='1'
}
$deadline = [DateTime]::UtcNow.AddSeconds(60)
do { Start-Sleep -Milliseconds 250 } while (!(Test-Path $screenshot) -and [DateTime]::UtcNow -lt $deadline)
if (!(Test-Path $screenshot)) { throw 'No restarted application screenshot.' }
if ((Get-Item "$current/Moonrise.exe").VersionInfo.ProductVersion -ne '0.1.0-beta.2') { throw 'Update did not increase installed version.' }
Start-Sleep -Seconds 3
foreach ($file in $before.Keys) { if ((Get-FileHash $file).Hash -ne $before[$file]) { throw "Data changed: $file" } }
Copy-Helper
Run $helper @(('"'+$feed+'"'),'none') 'no-repeat'
Run $helper @(('"'+$data+'"'),'verify') 'verify-data'
$newJar = Join-Path $current 'runtime/catalog/Veyra-1.0.0-moonrise.2.jar'
if ((Get-FileHash $newJar).Hash -ne '22B8788650D74C8ADDECE463E376FE9700F0084DCB3492F931D0FAD12CC23427') { throw 'New catalog JAR mismatch.' }
@{ Status='PASS'; Source=$feed; From='0.1.0-beta.1'; To='0.1.0-beta.2'; Apply='UpdateService and native Update.exe'; Restart='WPF QA screenshot'; Data='Settings and enabled library reloaded, protected hashes unchanged'; Repeat='No update'; Limitations='Interactive UI, tray, Minecraft and rollback not tested' } |
    ConvertTo-Json | Tee-Object -FilePath "$root/result.json"
