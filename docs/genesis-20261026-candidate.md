# Sorel 1.0.2 candidate — 2026-10-26

Base: release/sorel-1.0.1, commit 71748a698878767ae9433a8915a4135722da2cbe.

Root cause: current Genesis renamed the game URLClassLoader to com/moonsworth/lunar/genesis/COIOIRHRRORRHHOIIORORRHOOCRHII. Adapter 1.1.0 only recognized the two prior names, leaving registered Weave Args synthetic classes unavailable. New adapter 1.2.0 recognizes this exact loader and the exact verified Genesis SHA-256 1CD59B4815B6276C4E6D66306FC2126DB054352E1A142F62F25860CC6AAEFBAD.

Validation completed:
- Offline Java integration against actual untouched Genesis and Weave 1.3.4: baseline failure reproduced, adapted Args class generation/definition/invocation, remapping, concurrent loading and unknown-class rejection passed with -Xverify:all.
- 771 public .NET tests passed; zero failed/skipped.
- Release single-file publish and Velopack 1.2.161 stable 1.0.2 packaging passed.
- Published executable QA startup: exit code 0, startup screenshot produced, isolated data directory.
- Update smoke helper built with zero warnings/errors; removed hardcoded 1.0.1 candidate expectation so CI can validate the real candidate feed version.

Not yet verified: real Lunar/Minecraft launch with Veyra and Cosmetics; installed 1.0.1-to-1.0.2 update on disposable Windows runner. The user explicitly authorized GitHub publication before runtime confirmation on 2026-10-26. Original account files and installed Sorel binaries were not changed. Existing unrelated app working tree was left unchanged.

Artifacts: artifacts/release-1.0.2/Sorel/Sorel.exe and artifacts/release-1.0.2/velopack/.