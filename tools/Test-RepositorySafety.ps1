param(
    [string]$PrivatePatterns = $env:SOREL_PRIVATE_PATTERNS
)

$ErrorActionPreference = "Stop"
$repository = Split-Path -Parent $PSScriptRoot
Push-Location $repository
try {
    $tracked = @(git ls-files --cached --others --exclude-standard) | Sort-Object -Unique
    if ($LASTEXITCODE -ne 0) { throw "Unable to enumerate repository files." }

    $approvedArtifacts = @{
        'runtime/main-cosmetics-build/sorel-cosmetics-1.3.2.jar' = 'CC066D1B755C01DB11D2B59E1858F699CED2CA3BEEFA57E113D05D30C6D67A04'
        'runtime/mixed-weave-adapter/sorel-mixed-agent-0.1.0.jar' = 'C762DCEFC0DD85742567790C29657D2205F6F046EEDFF84CD22463DD2B4215F9'
        'runtime/mixed-weave-adapter/sorel-mixed-raven-abi-0.1.0.jar' = 'BEF8F65CDED547A4094B2A3727210E90B397661A3288EA584A77128B3F76C0E0'
        'runtime/catalog/Veyra-1.0.1.jar' = '38927237E09C764D20EE747C9276CB8CAA29EC8CE5A336BD93C2DCAE930C9772'
        'extensions/MixedWeave/Weave-Loader-0.2.6-source.zip' = 'FE9EFB97F292DEB931923094F755D1EF59E8C6696B246CED0A32497648D1F711'
    }
    foreach ($file in $approvedArtifacts.Keys) {
        if (-not (Test-Path -LiteralPath $file) -or (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $approvedArtifacts[$file]) {
            throw "Reviewed artifact missing or hash mismatch: $file"
        }
    }
    $forbidden = $tracked | Where-Object {
        $_ -match '^(user-mods|user-agents|profiles|plugins|language-packs|themes|logs|crash-reports|release|artifacts?)(/|$)' -or
        $_ -match '(?i)(^|/)(account|accounts|session|sessions|token|tokens|credential|credentials|settings)(\.[^/]*)?$' -or
        ($_ -match '(?i)\.(jar|pfx|p12|pem|key|snk|log|dmp|mdmp|zip|7z|rar)$' -and
            $_ -ne 'runtime/adapters/sorel-bwh-exitlag-network-agent.jar' -and -not $approvedArtifacts.ContainsKey($_)) -or
        ($_ -match '(?i)\.(exe|dll)$' -and $_ -ne 'runtime/bridge/Sorel.Native.dll')
    }
    if ($forbidden) {
        $forbidden | ForEach-Object { Write-Error "Forbidden tracked path: $_" }
        throw "Repository safety scan found forbidden tracked files."
    }

    $patterns = @($PrivatePatterns -split '[,;\r\n]+' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    $secretSignature = '(gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}|sk-(?:proj-)?[A-Za-z0-9_-]{30,}|-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|eyJ[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{15,}\.[A-Za-z0-9_-]{15,})'
    foreach ($file in $tracked) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf) -or
            $file -match '(?i)\.(dll|exe|jar|png|ico|jpg|jpeg|gif|woff2?|ttf|zip|pdf)$') { continue }
        $content = [IO.File]::ReadAllText((Join-Path $repository $file))
        if ($content -match $secretSignature) {
            throw "Potential secret signature in $file (value withheld)."
        }
        if ($file -eq 'tools/Test-RepositorySafety.ps1') { continue }
        foreach ($pattern in $patterns) {
            if ($content.IndexOf($pattern, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Configured private-name pattern matched in $file (value withheld)."
            }
        }
    }

    Write-Host "Repository safety scan passed for $($tracked.Count) tracked/untracked files."
}
finally {
    Pop-Location
}
