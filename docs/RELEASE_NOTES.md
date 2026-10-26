# Sorel v1.0.2

Windows x64 maintenance update through the existing Velopack stable channel.

- Updated the separate Weave synthetic-class adapter for the Genesis runtime observed on 2026-10-26 (SHA-256 1CD59B48...).
- Fixed the missing generated Args$1 class loading boundary caused by the renamed Genesis game loader.
- Kept exact runtime verification; unknown Genesis builds remain rejected.

The adapter is verified offline against untouched Weave 1.3.4 and the actual Genesis runtime, including generated class execution and concurrent loading. Full Minecraft startup with enabled packages requires a real launch check. Original package JARs and account files are not modified.