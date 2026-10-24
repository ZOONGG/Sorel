# Application updates

## Current Velopack beta path

The current Settings → Updates UI uses Velopack SDK **1.2.161**. It checks the public [ZOONGG/Sorel GitHub Releases](https://github.com/ZOONGG/Sorel/releases) source without a token, includes published prereleases and explicitly selects channel **beta**. Only newer `-stable versions` versions are staged; downgrades are disabled. The stable/prerelease preference retained in settings does not switch this service to a stable channel.

- Startup checks run when `CheckForUpdates` is enabled (default: true); manual checks are available in Settings.
- `AutoDownloadUpdates` defaults to true. A detected update downloads in the background with visible progress; disabling this setting exposes manual Download.
- Velopack verifies/stages its packages in the installation package directory, outside Sorel user data. Download failure leaves an error and allows retry.
- Notifications distinguish available and downloaded updates. Downloading alone does not replace the running version.
- Install and restart uses Sorel's activity/game guard. A pending update also attempts guarded application before WPF startup on the next launch. Unconditional Velopack startup auto-apply is disabled.
- Active launches, tracked games/Minecraft or Lunar JVMs, and current Sorel operations block application. Unrelated Java programs are not intentionally treated as Minecraft. Tray resources are disposed before the UI-triggered handoff; synchronous handoff errors restore the tray and show an error.

## Eligibility and publication

Updates require a real, non-portable Velopack installation. Inno Setup, ZIP, development/uninstalled and FriendPortable builds do not enable this path. A `Sorel.portable` marker does not turn an extracted ZIP into an updateable installation.

The owner must manually publish a matching beta package/feed to GitHub, including `releases.stable.json` and the full `.nupkg` assets. Public GitHub/network access is required. No public release is currently published. Production `New-SorelRelease.ps1 -Velopack` packages **Sorel.App** (display name Sorel), separate from the Sorel user-data directory. Publish the generated stable feed and full package together. Supplying LocalUpdateFeed instead produces isolated **Sorel.UpdateSmoke.App** acceptance packages; never publish them.

Test builds with embedded `SorelLocalUpdateFeed` use only that local SimpleFileSource and do not fall back to GitHub. They use a feed-specific isolated data root and must not be published as production builds.

## Data and verification limits

Normal builds keep settings and packages under `%LOCALAPPDATA%\Sorel`, separate from installed application files. The updater does not migrate or replace that data root. A real isolated installed beta.1 → beta.2 smoke passed through Sorel UpdateService, the Windows locator and native Update.exe, including rejection of a corrupt download, restart, no-repeat check and preservation of external sentinel hashes. The restarted UI used isolated visual QA data; interactive settings/library loading, tray handoff and automatic rollback remain unverified in this pass. See [final validation](final-release-validation.md) and the [manual acceptance plan](installed-update-smoke.md). Do not describe automatic rollback as proven. Prepared packages are unsigned.

The older Setup-download service and stable/prerelease settings remain in source; they are not the current Velopack UI path. The [update foundation report](github-update-foundation.md) records an earlier milestone before UI wiring and is historical.

Implementation references: `App.xaml.cs`, `MainWindow.xaml.cs`, `Services/UpdateService.cs`, `Services/UpdateUiState.cs`, and `Models/SorelSettings.cs`. [Velopack documentation](https://docs.velopack.io/integrating/overview) describes the upstream framework; Sorel behavior above is based on its current source.
