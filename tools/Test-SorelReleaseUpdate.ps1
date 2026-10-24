param([Parameter(Mandatory)][string]$SetupPath,[Parameter(Mandatory)][string]$FeedDirectory)
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Run only on a disposable GitHub Actions Windows runner.' }
$repo = Split-Path $PSScriptRoot -Parent
$root = Join-Path $repo 'artifacts/stable-update-smoke'
$install = Join-Path $root 'install'
$data = Join-Path $root 'data'
$helperOutput = Join-Path $root 'helper'
New-Item -ItemType Directory $root,$data,$helperOutput -Force | Out-Null
if (Test-Path 'Registry::HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\Sorel.App') { throw 'Existing Sorel installation on runner.' }
$setup = (Resolve-Path -LiteralPath $SetupPath).Path
function Run($exe, $arguments, $name, $environment = @{}) {
    $p = Start-Process $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru -Environment $environment `
        -RedirectStandardOutput "$root/$name.stdout.txt" -RedirectStandardError "$root/$name.stderr.txt"
    if (!$p.WaitForExit(180000) -or $p.ExitCode -ne 0) {
        Get-Content "$root/$name.stdout.txt", "$root/$name.stderr.txt" -ErrorAction SilentlyContinue
        throw "$name failed"
    }
}
Run $setup @('--silent','--installto',('"'+$install+'"')) 'install-1.0.0'
$current = Join-Path $install 'current'
if ((Get-Item "$current/Sorel.exe").VersionInfo.ProductVersion -ne '1.0.0') { throw 'Old installed version mismatch.' }
dotnet publish "$repo/tools/ReleaseUpdateSmoke/ReleaseUpdateSmoke.csproj" -c Release -p:RestoreLockedMode=true -o $helperOutput
if ($LASTEXITCODE) { throw 'Helper build failed.' }
function Copy-Helper {
    Get-ChildItem $helperOutput -File | Where-Object Name -ne 'Sorel.exe' | ForEach-Object { Copy-Item $_.FullName $current -Force }
}
Copy-Helper
$helper = Join-Path $current 'ReleaseUpdateSmoke.exe'
Run $helper @(('"'+$data+'"'),'seed',('"'+$current+'/runtime/catalog/Veyra-1.0.1.jar"')) 'seed'
# Establish the data state of a previously launched beta.1. Its normal settings
# save already normalizes the obsolete UpdateCatalogAutomatically flag to false.
Run "$current/Sorel.exe" @('--release-smoke') 'launch-1.0.0' @{
    SOREL_VISUAL_QA='1'; SOREL_VISUAL_QA_DATA_ROOT=$data
    SOREL_VISUAL_QA_SCREENSHOT="$root/baseline-1.0.0.png"; SOREL_VISUAL_QA_EXIT_AFTER_SCREENSHOT='1'
}
if (!(Test-Path "$root/baseline-1.0.0.png")) { throw 'Old installed application did not launch.' }
$protected = @(Get-ChildItem "$data/settings", "$data/packages" -Recurse -File | Where-Object { $_.Extension -eq '.jar' -or $_.Name -eq 'sorel-settings.json' })
$before = @{}; foreach ($file in $protected) { $before[$file.FullName] = (Get-FileHash $file.FullName).Hash }
$feed = (Resolve-Path -LiteralPath $FeedDirectory).Path
$screenshot = Join-Path $root 'restarted-1.0.1.png'
Run $helper @(('"'+$feed+'"'),'apply') 'apply' @{
    SOREL_VISUAL_QA='1'; SOREL_VISUAL_QA_DATA_ROOT=$data
    SOREL_VISUAL_QA_SCREENSHOT=$screenshot; SOREL_VISUAL_QA_EXIT_AFTER_SCREENSHOT='1'
}
$deadline = [DateTime]::UtcNow.AddSeconds(60)
do { Start-Sleep -Milliseconds 250 } while (!(Test-Path $screenshot) -and [DateTime]::UtcNow -lt $deadline)
if (!(Test-Path $screenshot)) { throw 'No restarted application screenshot.' }
if ((Get-Item "$current/Sorel.exe").VersionInfo.ProductVersion -ne '1.0.1') { throw 'Update did not increase installed version.' }
Start-Sleep -Seconds 3
foreach ($file in $before.Keys) { if ((Get-FileHash $file).Hash -ne $before[$file]) { throw "Data changed: $file" } }
Copy-Helper
Run $helper @(('"'+$feed+'"'),'none') 'no-repeat'
Run $helper @(('"'+$data+'"'),'verify') 'verify-data'
$newJar = Join-Path $current 'runtime/catalog/Veyra-1.0.1.jar'
if ((Get-FileHash $newJar).Hash -ne '38927237E09C764D20EE747C9276CB8CAA29EC8CE5A336BD93C2DCAE930C9772') { throw 'New catalog JAR mismatch.' }
@{ Status='PASS'; Source=$feed; From='1.0.0'; To='1.0.1'; Apply='UpdateService and native Update.exe'; Restart='WPF QA screenshot'; Data='Settings and enabled library reloaded, protected hashes unchanged'; Repeat='No update'; Limitations='Interactive UI, tray, Minecraft and rollback not tested' } |
    ConvertTo-Json | Tee-Object -FilePath "$root/result.json"
