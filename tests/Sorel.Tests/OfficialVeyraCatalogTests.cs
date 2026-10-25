using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using Sorel.Services;
using Xunit;

namespace Sorel.Tests;

public sealed class OfficialVeyraCatalogTests
{
    private static Sorel.Models.PackageManifest Bundled => PackageCatalogService.LoadBundledCatalog().Single(p => p.Id == "veyra");

    [Fact]
    public async Task PublishedNewVersionUpdatesOnlyMetadataAndUsesOfficialDigest()
    {
        using var http = new HttpClient(new ResponseHandler(Payload()));
        var latest = await new OfficialVeyraCatalogClient(http).GetLatestAsync(Bundled);
        Assert.Equal("1.0.2", latest.VersionLabel);
        Assert.Equal("veyra", latest.Id);
        Assert.True(latest.CanInstall);
        Assert.Equal(new string('a', 64), latest.LatestRelease!.Artifact.Sha256);
        Assert.Equal("untested · official Veyra release", latest.CompatibilityLabel);
        Assert.Equal("1.0.1", Bundled.VersionLabel);
    }

    [Fact]
    public void SamePublishedVersionKeepsOfflineBundleAndOlderVersionCannotDowngrade()
    {
        var bundled = Bundled;
        var same = Payload("1.0.1", bundled.LatestRelease!.Artifact.Sha256!);
        Assert.Same(bundled, OfficialVeyraCatalogClient.Parse(same, bundled));
        var newer = OfficialVeyraCatalogClient.Parse(Payload(), bundled);
        Assert.Same(newer, OfficialVeyraCatalogClient.Parse(same, newer));
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("prerelease")]
    [InlineData("missing-date")]
    [InlineData("missing-hash")]
    [InlineData("wrong-repository")]
    [InlineData("wrong-size")]
    [InlineData("missing-jar")]
    [InlineData("ambiguous-jar")]
    [InlineData("same-version-changed-hash")]
    public void InvalidOrUnpublishedReleaseIsRejected(string problem)
    {
        var node = JsonNode.Parse(Payload(problem == "same-version-changed-hash" ? "1.0.1" : "1.0.2"))!;
        var assets = node["assets"]!.AsArray();
        switch (problem)
        {
            case "draft": node["draft"] = true; break;
            case "prerelease": node["prerelease"] = true; break;
            case "missing-date": node["published_at"] = null; break;
            case "missing-hash": assets[0]!["digest"] = ""; break;
            case "wrong-repository": assets[0]!["browser_download_url"] = "https://github.com/other/repo/releases/download/v1.0.2/Veyra-1.0.2.jar"; break;
            case "wrong-size": assets[0]!["size"] = -1; break;
            case "missing-jar": assets.Clear(); break;
            case "ambiguous-jar": assets.Add(assets[0]!.DeepClone()); break;
        }
        Assert.Throws<InvalidDataException>(() => OfficialVeyraCatalogClient.Parse(System.Text.Encoding.UTF8.GetBytes(node.ToJsonString()), Bundled));
    }

    [Fact]
    public async Task OfflineFailureDoesNotReplaceExistingCatalog()
    {
        var bundled = Bundled;
        using var http = new HttpClient(new ResponseHandler([], HttpStatusCode.ServiceUnavailable));
        await Assert.ThrowsAsync<HttpRequestException>(() => new OfficialVeyraCatalogClient(http).GetLatestAsync(bundled));
        Assert.Equal("1.0.1", bundled.VersionLabel);
        Assert.Equal(Sorel.Models.PackageDownloadMode.Bundled, bundled.LatestRelease!.Artifact.DownloadMode);
    }

    private static byte[] Payload(string version = "1.0.2", string? hash = null) => System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
    {
        tag_name = "v" + version, draft = false, prerelease = false, published_at = "2026-10-25T00:00:00Z",
        assets = new[] { new
        {
            name = $"Veyra-{version}.jar", size = 123,
            digest = "sha256:" + (hash ?? new string('a', 64)),
            browser_download_url = $"https://github.com/ZOONGG/Sorel-Veyra/releases/download/v{version}/Veyra-{version}.jar"
        } }
    });

    private sealed class ResponseHandler(byte[] payload, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(OfficialVeyraCatalogClient.LatestReleaseUrl, request.RequestUri!.AbsoluteUri);
            Assert.NotEmpty(request.Headers.UserAgent);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new ByteArrayContent(payload) });
        }
    }
}
