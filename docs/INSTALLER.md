# Windows installation

The first public beta uses Velopack 1.2.161: `Sorel.App-stable-Setup.exe`.
Package ID `Sorel.App`, display name Sorel, channel `beta`, version `1.0.0`.
The ID deliberately differs from the data directory to prevent install/update/uninstall from owning user data.
Default installation: `%LOCALAPPDATA%\Sorel.App`; settings and packages: `%LOCALAPPDATA%\Sorel`.
Setup is per-user, self-contained win-x64 and unsigned. No administrator rights or separate .NET runtime are required.
Check its SHA-256, run Setup, then use the Sorel shortcut. Normal uninstall uses Windows Installed Apps.
Do not install into the Sorel data directory. Old Inno installations are a separate distribution; they do not gain Velopack updates automatically.
The Velopack installer does not promise the old Inno wizard or sorel:// registration.

Build using the pinned local CLI:

```powershell
dotnet tool install vpk --version 1.2.161 --tool-path artifacts/velopack-tools
./tools/New-SorelRelease.ps1 -Version 1.0.0 -Velopack -VpkPath artifacts/velopack-tools/vpk.exe -OutputRoot artifacts/final-package
```

The script verifies the bundled runtime JAR digests and corresponding source archive before packaging.
Production builds must omit LocalUpdateFeed. Builds that specify it are local-only Sorel.UpdateSmoke packages and must never be published.
See [updates](UPDATES.md), [ZIP behavior](PORTABLE_MODE.md), and [final validation](final-release-validation.md).
