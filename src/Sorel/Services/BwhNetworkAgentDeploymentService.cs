using System.Security.Cryptography;
using Sorel.Infrastructure;

namespace Sorel.Services;

public sealed class BwhNetworkAgentDeploymentService
{
    internal const string ResourceName = "Sorel.BwhNetworkAgent.jar";
    internal const string ExpectedSha256 = "E36128BCAEF56FC2C705D00603EB9781C2DB55DFF5540DB38AF5E31BF55B90D2";
    private readonly AppPaths _paths;

    public BwhNetworkAgentDeploymentService(AppPaths paths)
    {
        _paths = paths;
    }

    public string Ensure()
    {
        var directory = Path.Combine(_paths.AdaptersDirectory, "technical");
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, "sorel-bwh-exitlag-network-agent.jar");
        if (File.Exists(destination) && Verify(destination))
            return destination;

        using var resource = typeof(BwhNetworkAgentDeploymentService).Assembly
            .GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Embedded BWH network agent is missing.");
        var temporary = destination + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                resource.CopyTo(output);
            if (!Verify(temporary))
                throw new InvalidDataException("Embedded BWH network agent failed its SHA-256 check.");
            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
        return destination;
    }

    private static bool Verify(string path)
    {
        using var stream = File.OpenRead(path);
        return string.Equals(
            Convert.ToHexString(SHA256.HashData(stream)),
            ExpectedSha256,
            StringComparison.OrdinalIgnoreCase);
    }
}
