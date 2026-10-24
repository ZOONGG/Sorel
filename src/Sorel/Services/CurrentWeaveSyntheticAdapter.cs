using System.IO.Compression;
using Sorel.Models;

namespace Sorel.Services;

// OfficialLunar current Weave only; adapter lifetime follows the existing launch session.
public static class CurrentWeaveSyntheticAdapter
{
    public const string Id = "sorel-current-weave-synthetic-1.1.0";
    public static LaunchPlan Prepare(LaunchPlan plan, string sessionDirectory)
    {
        if (plan.WeaveMode != WeaveRuntimeMode.Current) return plan;
        var weave = plan.Agents.Single(a => a.IsWeaveLoader);
        var genesis = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".lunarclient", "offline", "multiver", "genesis-0.1.0-SNAPSHOT-all.jar");
        ValidateHash(weave.Path, "013b7b9a4ca01a473a1ffc0b031c843c70fe61616e9048fb326e03794c8457b7");
        ValidateGenesisHash(genesis);
        var adapter = Path.Combine(sessionDirectory, Id + ".jar");
        WriteJar(adapter);
        var agents = plan.Agents.ToList();
        agents.Insert(0, new LaunchAgent(adapter, weave.Path, Id, LaunchAgentRole.CompatibilityAdapter));
        return plan with { Agents = agents };
    }
    internal static void ValidateHash(string path, string hash)
    {
        if (!File.Exists(path) || !LocalPackageLibrary.ComputeSha256(path).Equals(hash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Current Weave synthetic bridge requires the verified Weave 1.3.4 / Genesis runtime: {Path.GetFileName(path)}.");
    }
    internal static void ValidateGenesisHash(string path)
    {
        // Exact builds verified by SyntheticIntegrationTest against their real game loader.
        var hash = File.Exists(path) ? LocalPackageLibrary.ComputeSha256(path) : "";
        if (!IsVerifiedGenesisHash(hash))
            throw new InvalidDataException(hash.Length == 0
                ? $"Current Weave synthetic bridge: Genesis runtime was not found: {Path.GetFileName(path)}. Launch Lunar 1.8.9 once to prepare its runtime."
                : $"Current Weave synthetic bridge: unverified Genesis runtime SHA-256={hash}. Sorel requires an adapter update for this Lunar runtime.");
    }
    internal static bool IsVerifiedGenesisHash(string hash) =>
        hash.Equals("e3f34c4ad92f1c3bae83c17bbacb45952c156df359cb39d6c34d086b05e83c44", StringComparison.OrdinalIgnoreCase)
        || hash.Equals("40797ad1d1449ffff7c6e7342b79a3edc8d08517505dc2d57ce9d97e0669389c", StringComparison.OrdinalIgnoreCase)
        || hash.Equals("b8dc53894251ba825c0b0a899eeac00db8cf311c9ba02e182f8650c4887b9cfc", StringComparison.OrdinalIgnoreCase)
        || hash.Equals("26fe6dd9d4a6c3630aca96c22faf52ed6d6fa4f772d23258e1e91f8289abb0ef", StringComparison.OrdinalIgnoreCase);

    internal static void WriteJar(string path)
    {
        using var file = new FileStream(path, FileMode.CreateNew);
        using var jar = new ZipArchive(file, ZipArchiveMode.Create);
        using (var writer = new StreamWriter(jar.CreateEntry("META-INF/MANIFEST.MF").Open()))
            writer.Write("Manifest-Version: 1.0\r\nPremain-Class: dev.sorel.current.CurrentSyntheticAgent\r\n\r\n");
        foreach (var item in CurrentSyntheticAdapterClasses.Bytes)
        {
            using var output = jar.CreateEntry(item.Key).Open(); output.Write(Convert.FromBase64String(item.Value));
        }
    }
}
