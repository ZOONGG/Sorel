using System.Text.Json;
using Sorel.Models;

namespace Sorel.Services;

public sealed class AppSettingsService(string path)
{
    private readonly string _path = Path.GetFullPath(path);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SorelSettings Load()
    {
        EnsureSupported();
        try
        {
            var settings = File.Exists(_path)
                ? JsonSerializer.Deserialize<SorelSettings>(File.ReadAllText(_path))
                  ?? throw new JsonException("The settings document is empty.")
                : new SorelSettings();
            if (settings.SettingsSchemaVersion < SorelSettings.CurrentSettingsSchemaVersion)
            {
                if (settings.SettingsSchemaVersion < 3)
                {
                    // Version 3 restores the bundled Sorel presentation once for
                    // existing installations. The theme picker remains available and
                    // later explicit choices are preserved.
                    settings.Theme = "standard";
                }
                if (settings.SettingsSchemaVersion < 5)
                {
                    // Background Lunar remains an internal launch behavior. The
                    // user-facing switch was removed, so normalize old values.
                    settings.BackgroundLunarLaunch = true;
                }
                if (settings.SettingsSchemaVersion < 6 &&
                    (string.Equals(settings.Theme, "obsidian", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(settings.Theme, "aurora", StringComparison.OrdinalIgnoreCase)))
                {
                    // V1 replaces legacy palette-only choices with full theme packs.
                    // Sorel Standard is the safe migration target for removed ids.
                    settings.Theme = "standard";
                }
                settings.SettingsSchemaVersion = SorelSettings.CurrentSettingsSchemaVersion;
                TrySaveMigration(settings);
            }
            return settings;
        }
        catch
        {
            return new SorelSettings();
        }
    }

    public void Save(SorelSettings settings)
    {
        EnsureSupported();
        settings.SettingsSchemaVersion = SorelSettings.CurrentSettingsSchemaVersion;
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporary = _path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporary, _path, true);
    }

    private void EnsureSupported() => Sorel.Infrastructure.StorageFormatGuard.CheckSettings(_path);

    private void TrySaveMigration(SorelSettings settings)
    {
        try
        {
            Save(settings);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            // The in-memory migration still applies to this launch. A later
            // successful settings save will persist the schema version.
        }
    }
}
