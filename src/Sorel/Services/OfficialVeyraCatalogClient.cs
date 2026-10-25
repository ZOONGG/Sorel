using System.Net.Http;
using System.Text.Json;
using Sorel.Models;

namespace Sorel.Services;

// Only the owner's published stable Veyra artifacts; no arbitrary repositories or executable feed data.
public sealed class OfficialVeyraCatalogClient(HttpClient http)
{
    public const string LatestReleaseUrl = "https://api.github.com/repos/ZOONGG/Sorel-Veyra/releases/latest";
    private const int MaximumMetadataBytes = 1024 * 1024;

    public async Task<PackageManifest> GetLatestAsync(PackageManifest current, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl);
        request.Headers.UserAgent.ParseAdd("Sorel/1.0.1");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaximumMetadataBytes)
            throw new InvalidDataException("Veyra release metadata is too large.");
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var payload = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await input.ReadAsync(buffer, cancellationToken)) != 0)
        {
            if (payload.Length + count > MaximumMetadataBytes) throw new InvalidDataException("Veyra release metadata is too large.");
            payload.Write(buffer, 0, count);
        }
        return Parse(payload.ToArray(), current);
    }

    internal static PackageManifest Parse(byte[] bytes, PackageManifest current)
    {
        if (current.Id != "veyra") throw new ArgumentException("Only the official Veyra entry may be updated.");
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean())
            throw new InvalidDataException("Veyra release is not stable/published.");
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        var versionText = tag.StartsWith('v') ? tag[1..] : tag;
        if (!Version.TryParse(versionText, out var version) || version.Build < 0 || version.Revision >= 0)
            throw new InvalidDataException("Unsupported Veyra release version.");
        if (!root.TryGetProperty("published_at", out var date) || date.ValueKind != JsonValueKind.String || !date.TryGetDateTimeOffset(out var published))
            throw new InvalidDataException("Veyra publication date is missing.");
        var name = $"Veyra-{versionText}.jar";
        var matches = root.GetProperty("assets").EnumerateArray()
            .Where(asset => asset.GetProperty("name").GetString() == name).ToArray();
        if (matches.Length != 1) throw new InvalidDataException("Official Veyra release JAR is missing or ambiguous.");
        var asset = matches[0];
        var expectedUrl = $"https://github.com/ZOONGG/Sorel-Veyra/releases/download/{tag}/{name}";
        if (asset.GetProperty("browser_download_url").GetString() != expectedUrl)
            throw new InvalidDataException("Veyra artifact must belong to its official release.");
        var digest = asset.GetProperty("digest").GetString() ?? "";
        if (!digest.StartsWith("sha256:", StringComparison.Ordinal) || digest.Length != 71 ||
            digest[7..].Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidDataException("Official Veyra SHA-256 is missing or invalid.");
        var size = asset.GetProperty("size").GetInt64();
        if (size <= 0 || size > LocalPackageLibrary.MaximumJarSize)
            throw new InvalidDataException("Veyra artifact size is invalid.");
        if (Version.TryParse(current.VersionLabel, out var known) && version <= known)
        {
            if (version == known && !digest[7..].Equals(current.LatestRelease!.Artifact.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Published Veyra hash changed without a new version.");
            return current;
        }
        return new PackageManifest
        {
            Id = current.Id, Slug = current.Slug, Name = current.Name, Type = current.Type,
            Summary = current.Summary, Description = current.Description, Authors = current.Authors,
            License = current.License, RiskLevel = current.RiskLevel,
            Repository = "https://github.com/ZOONGG/Sorel-Veyra",
            InstallPolicy = PackageInstallPolicy.OneClick, CompatibilityLabel = "untested · official Veyra release",
            Releases = [new PackageRelease
            {
                Version = versionText, PublishedAt = published,
                Artifact = new PackageArtifact
                {
                    DownloadMode = PackageDownloadMode.GithubRelease, AssetUrl = expectedUrl,
                    FileName = name, FileSize = size, Sha256 = digest[7..]
                }
            }]
        };
    }
}
