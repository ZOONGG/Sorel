# Sorel automatic updates

Sorel uses Velopack SDK 1.2.161, package ID `Sorel.App` and explicit channel `stable`.
`GithubSource` reads https://github.com/ZOONGG/Sorel with prereleases disabled and no token.
The stable feed is `releases.stable.json`. Publish it together with the exact full `.nupkg` named by the feed; filenames, sizes and SHA-256 must match. Do not rename the assets after packaging.

Installed versions check automatically when Check for Updates is enabled. Auto Download stages a verified package. The UI offers restart once it is ready. Startup and restart guards prevent application during an active Minecraft/launch operation. Portable ZIP builds do not support integrated updates.

Settings and package originals live under `%LOCALAPPDATA%\Sorel`, outside the installation. Updates preserve this directory. Sorel is independent of previous products and performs no migration or deletion of their data.

The acceptance workflow downloads and checks the SHA-256 of the actual published Sorel 1.0.0 installer. It consumes the exact Velopack artifacts from a successful Build run of the same commit, installs 1.0.0 on a disposable Windows runner, downloads/stages through UpdateService, applies with native Update.exe and restarts the actual WPF application. It checks settings, enabled library, JAR hashes and no-repeat behavior. Dispatch update-smoke.yml with build_run_id after Build succeeds. Publish only the validated production artifacts, never the isolated installation, test data or locally relabelled test packages. Interactive update UI, tray, Minecraft and rollback remain separate runtime checks.

Veyra catalog freshness is independent of Sorel updates: the application checks the official ZOONGG/Sorel-Veyra latest stable GitHub Release at startup, on returning to the window or opening Catalog (throttled to five minutes), and every five minutes while Catalog is visible. It accepts only an exact versioned Veyra JAR from that repository with the official asset SHA-256 and size. New releases are labelled untested until independently verified. No installed package is automatically replaced. Failed checks retain current entries with an explicit offline/stale message; bundled entries remain available offline. A local unpublished JAR is not a published catalog release.
