# Support UI development

Sorel Support uses a public API base URL. The desktop app contains no Telegram bot token,
webhook secret, provider credential, or shared API secret. Debug and other development builds
default to `http://127.0.0.1:8787`; Release builds default to
`https://sorel-backend-production.up.railway.app`. The selected build-time endpoint is embedded
as assembly metadata, so a copied single-file executable keeps working without a companion
configuration file.

For local testing, start the private backend on loopback port `8787`, then launch Sorel from the
same PowerShell session:

```powershell
$env:SOREL_SUPPORT_API_BASE_URL = 'http://127.0.0.1:8787'
dotnet run --project src/Sorel/Sorel.csproj
```

To override the embedded default for one machine, place this public configuration beside
`Sorel.exe`:

```json
{"baseUrl":"http://127.0.0.1:8787"}
```

Name the file `sorel-support.json`. Runtime resolution is, in descending priority:

1. `SOREL_SUPPORT_API_BASE_URL`;
2. the AppContext override and `sorel-support.json` development override;
3. the build-time `SorelSupportApiBaseUrl` value, including the configuration-specific default.

Loopback HTTP is accepted only for development. Non-loopback endpoints must use HTTPS. A publish
can override either configuration default explicitly, for example
`-p:SorelSupportApiBaseUrl=https://support.example.com`.

The checkout capability token remains in memory for the active modal only. Closing Support,
changing the amount, or exiting Sorel cancels polling and releases the token reference.
