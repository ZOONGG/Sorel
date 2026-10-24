param([Parameter(Mandatory)][string]$SetupPath,[Parameter(Mandatory)][string]$FeedDirectory)
& "$PSScriptRoot/Test-SorelReleaseUpdate.ps1" -SetupPath $SetupPath -FeedDirectory $FeedDirectory