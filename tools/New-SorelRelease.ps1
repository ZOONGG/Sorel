param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,

    [string]$OutputRoot = "release",
    [string]$DotNetPath = "dotnet",
    [string]$IsccPath = "",
    [switch]$SkipRestore,
    [switch]$SkipTests,
    [switch]$Velopack,
    [string]$VpkPath = "",
    [string]$LocalUpdateFeed = ""
)

$ErrorActionPreference = "Stop"
$repository = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$output = if ([IO.Path]::IsPathRooted($OutputRoot)) {
    [IO.Path]::GetFullPath($OutputRoot)
} else {
    [IO.Path]::GetFullPath((Join-Path $repository $OutputRoot))
}
$repositoryPrefix = $repository.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $output.StartsWith($repositoryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputRoot must stay inside the Sorel repository."
}
if ($output -eq $repository) {
    throw "OutputRoot cannot be the repository root."
}

if (-not $Velopack -and [string]::IsNullOrWhiteSpace($IsccPath)) {
    $isccCandidates = @(
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe")
    )
    $IsccPath = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $Velopack -and ([string]::IsNullOrWhiteSpace($IsccPath) -or -not (Test-Path -LiteralPath $IsccPath))) {
    throw "Inno Setup 6 compiler (ISCC.exe) was not found."
}

Push-Location $repository
try {
    if (Test-Path -LiteralPath $output) {
        $resolvedOutput = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $output))
        if (-not $resolvedOutput.StartsWith($repositoryPrefix, [StringComparison]::OrdinalIgnoreCase) -or
            $resolvedOutput -eq $repository) {
            throw "Refusing to remove unsafe release output path: $resolvedOutput"
        }
        Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
    }
    New-Item -ItemType Directory -Path $output | Out-Null

    if (-not $SkipRestore) {
        & $DotNetPath restore Sorel.sln --locked-mode
        if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE." }
    }
    if (-not $SkipTests) {
        & $DotNetPath test Sorel.sln -c Release --no-restore
        if ($LASTEXITCODE -ne 0) { throw "dotnet test failed with exit code $LASTEXITCODE." }
    }

    $publish = Join-Path $output "Sorel"
    $fourPartVersion = ($Version -split '-')[0] + '.0'
    & $DotNetPath publish src/Sorel/Sorel.csproj `
        -c Release `
        -r win-x64 `
        --self-contained true `
        --no-restore `
        -p:Version=$Version `
        -p:FileVersion=$fourPartVersion `
        -p:AssemblyVersion=$fourPartVersion `
        -p:InformationalVersion=$Version `
        -p:IncludeSourceRevisionInInformationalVersion=false `
        "-p:SorelLocalUpdateFeed=$LocalUpdateFeed" `
        -o $publish
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

    # Only these reproducible Sorel runtime artifacts may accompany the EXE.
    # Never admit imported originals or arbitrary JARs by extension alone.
    $runtimeJars = @{
        'runtime\catalog\Veyra-1.0.1.jar' = '38927237E09C764D20EE747C9276CB8CAA29EC8CE5A336BD93C2DCAE930C9772'
        'runtime\maintained-builds\sorel-cosmetics-1.3.2.jar' = 'CC066D1B755C01DB11D2B59E1858F699CED2CA3BEEFA57E113D05D30C6D67A04'
        'runtime\mixed-weave-adapter\sorel-mixed-agent-0.1.0.jar' = 'C762DCEFC0DD85742567790C29657D2205F6F046EEDFF84CD22463DD2B4215F9'
        'runtime\mixed-weave-adapter\sorel-mixed-raven-abi-0.1.0.jar' = 'BEF8F65CDED547A4094B2A3727210E90B397661A3288EA584A77128B3F76C0E0'
    }
    $forbidden = @(Get-ChildItem -LiteralPath $publish -Recurse -File | Where-Object {
        $relative = [IO.Path]::GetRelativePath($publish, $_.FullName)
        ($_.Extension -eq '.jar' -and -not $runtimeJars.ContainsKey($relative)) -or
        ($_.Extension -eq '.zip' -and $relative -ne 'licenses\mixed-weave\Weave-Loader-0.2.6-source.zip') -or
        $_.Extension -in ".log", ".dmp", ".mdmp", ".pdb", ".pfx", ".p12", ".pem", ".key" -or
        $_.Name -match "(?i)(account|credential|password|session|settings|token)"
    })
    if ($forbidden.Count -gt 0) {
        $forbidden | ForEach-Object { Write-Error "Forbidden publish entry: $($_.FullName)" }
        throw "Release safety check failed."
    }
    foreach ($relative in $runtimeJars.Keys) {
        $jar = Join-Path $publish $relative
        if (-not (Test-Path -LiteralPath $jar -PathType Leaf) -or
            (Get-FileHash -LiteralPath $jar -Algorithm SHA256).Hash -ne $runtimeJars[$relative]) {
            throw "Required runtime JAR is missing or has an unexpected SHA-256: $relative"
        }
    }
    $sourceArchive = Join-Path $publish 'licenses/mixed-weave/Weave-Loader-0.2.6-source.zip'
    if (-not (Test-Path -LiteralPath $sourceArchive) -or
        (Get-FileHash -LiteralPath $sourceArchive -Algorithm SHA256).Hash -ne 'FE9EFB97F292DEB931923094F755D1EF59E8C6696B246CED0A32497648D1F711') {
        throw 'Mixed adapter corresponding source archive is missing or has an unexpected hash.'
    }
    foreach ($required in "Sorel.exe", "README.md", "LICENSE", "THIRD_PARTY_NOTICES.md", "runtime\bridge\Sorel.Native.dll") {
        if (-not (Test-Path -LiteralPath (Join-Path $publish $required))) {
            throw "Required release entry is missing: $required"
        }
    }

    if ($Velopack) {
        if (-not (Test-Path -LiteralPath $VpkPath)) { throw "VpkPath must point to pinned vpk 1.2.161." }

        $packId = if ($LocalUpdateFeed) { 'Sorel.UpdateSmoke.App' } else { 'Sorel.App' }
        $packTitle = if ($LocalUpdateFeed) { 'Sorel Update Smoke' } else { 'Sorel' }
        & $VpkPath pack --packId $packId --packVersion $Version --packDir $publish `
            --mainExe Sorel.exe --channel stable --runtime win-x64 --delta None `
            --packTitle $packTitle --packAuthors ZOONGG `
            --releaseNotes (Join-Path $repository 'docs/RELEASE_NOTES.md') `
            --icon (Join-Path $repository 'assets/branding/sorel-icon.ico') --outputDir (Join-Path $output 'velopack')
        if ($LASTEXITCODE -ne 0) { throw "Velopack packaging failed." }
        $assets = @(Get-ChildItem -LiteralPath (Join-Path $output 'velopack') -File)
        foreach ($asset in $assets) {
            $hash = (Get-FileHash -LiteralPath $asset.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            Set-Content -LiteralPath "$($asset.FullName).sha256" -Value "$hash  $($asset.Name)" -Encoding ascii
        }
        return
    }
    $portableStage = Join-Path $output ".portable-stage"
    Copy-Item -LiteralPath $publish -Destination $portableStage -Recurse
    New-Item -ItemType File -Path (Join-Path $portableStage "Sorel.portable") | Out-Null
    $portableName = "Sorel-Portable-$Version-x64.zip"
    $portablePath = Join-Path $output $portableName
    & (Join-Path $PSScriptRoot "New-DeterministicArchive.ps1") `
        -SourceDirectory $portableStage `
        -DestinationPath $portablePath
    if ($LASTEXITCODE -ne 0) { throw "Portable archive creation failed." }
    Remove-Item -LiteralPath $portableStage -Recurse -Force

    & $IsccPath `
        "/DAppVersion=$Version" `
        "/DSourceDir=$publish" `
        "/DOutputDir=$output" `
        (Join-Path $repository "installer\Sorel.iss")
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed with exit code $LASTEXITCODE." }

    $setupName = "Sorel-Setup-$Version-x64.exe"
    $setupPath = Join-Path $output $setupName
    if (-not (Test-Path -LiteralPath $setupPath)) {
        throw "Expected installer was not produced: $setupPath"
    }

    foreach ($artifact in @($setupPath, $portablePath)) {
        $name = Split-Path -Leaf $artifact
        $hash = (Get-FileHash -LiteralPath $artifact -Algorithm SHA256).Hash.ToLowerInvariant()
        Set-Content -LiteralPath "$artifact.sha256" -Value "$hash  $name" -Encoding ascii -NoNewline
    }

    $notesName = "Sorel-Release-Notes-$Version.md"
    Copy-Item -LiteralPath (Join-Path $repository "docs\RELEASE_NOTES.md") -Destination (Join-Path $output $notesName)

    [pscustomobject]@{
        Setup = $setupPath
        SetupChecksum = "$setupPath.sha256"
        Portable = $portablePath
        PortableChecksum = "$portablePath.sha256"
        ReleaseNotes = Join-Path $output $notesName
        PublishDirectory = $publish
    } | Format-List
}
finally {
    Pop-Location
}
