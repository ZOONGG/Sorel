# Install Sorel 1.0.0

Download the stable release from https://github.com/ZOONGG/Sorel/releases/tag/v1.0.0.

- `Sorel.App-stable-Setup.exe`: self-contained Windows x64 installer. Installs Sorel with package ID `Sorel.App`, creates Sorel shortcuts and enables future stable updates.
- `Sorel.App-stable-Portable.zip`: extract the complete archive and run `Sorel.exe`. Integrated updates are unavailable for ZIP installations.
- `SHA256SUMS.txt`: compare the download hash before installation.

Sorel stores settings, logs and package originals under `%LOCALAPPDATA%\Sorel`. It does not remove existing application installations or their user data. Official Lunar Launcher and authentication are still required; Sorel does not manage accounts. The installer is unsigned.

Build with `tools/New-SorelRelease.ps1 -Version 1.0.0 -Velopack -VpkPath artifacts/velopack-tools/vpk.exe`. Pin vpk 1.2.161. Keep every generated feed/package filename unchanged. See [Updates](UPDATES.md).