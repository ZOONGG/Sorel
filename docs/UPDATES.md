# Sorel automatic updates

Sorel uses Velopack SDK 1.2.161, package ID `Sorel.App` and explicit channel `stable`.
`GithubSource` reads https://github.com/ZOONGG/Sorel with prereleases disabled and no token.
The stable feed is `releases.stable.json`. Publish it together with the exact full `.nupkg` named by the feed; filenames, sizes and SHA-256 must match. Do not rename the assets after packaging.

Installed versions check automatically when Check for Updates is enabled. Auto Download stages a verified package. The UI offers restart once it is ready. Startup and restart guards prevent application during an active Minecraft/launch operation. Portable ZIP builds do not support integrated updates.

Settings and package originals live under `%LOCALAPPDATA%\Sorel`, outside the installation. Updates preserve this directory. Sorel is independent of previous products and performs no migration or deletion of their data.

The acceptance workflow builds Sorel 1.0.0 and a local-only 1.0.1, installs 1.0.0 on a disposable Windows runner, downloads/stages through UpdateService, applies with native Update.exe and restarts the actual WPF application. It checks settings, enabled library, JAR hashes and no-repeat behavior. Test 1.0.1 packages must never be uploaded to GitHub Releases. This local test is not a public future-update delivery test. Interactive update UI, tray, Minecraft and rollback require separate runtime testing.