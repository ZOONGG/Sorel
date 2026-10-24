# Moonrise 0.1.0-beta.2

- Bundles Veyra 1.0.0-moonrise.2 with Timer exactly matching GitHub main; local Freeze experiments are excluded. Catalog metadata pins the published SHA-256.
- Supports the newly offline-verified Genesis runtime 26fe6dd9d4a6c3630aca96c22faf52ed6d6fa4f772d23258e1e91f8289abb0ef through a separate synthetic adapter; original JARs remain unchanged.
- Fixes approved prelaunch closure of user-started Lunar after restarting Moonrise. Automatic post-game cleanup still requires process ownership. Active-game guards and no-force policy remain.
- Keeps Velopack 1.2.161, Moonrise.App and beta channel. Correct releases.beta.json and matching full package support updates from installed beta.1. Auto Download stages the package; restart remains guarded.
- Retains Cosmetics 1.3.2, Mixed Weave Adapter, user packages and settings.

Use the installed application's updater. Setup is for new installations; ZIP builds do not support integrated updates. Minecraft/server runtime was not tested. Existing credits, upstream notices and distribution permissions remain.