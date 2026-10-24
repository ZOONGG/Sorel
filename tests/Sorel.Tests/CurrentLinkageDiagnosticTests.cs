using Sorel.Models;
using Sorel.Services;
using Xunit;

namespace Sorel.Tests;
public sealed class CurrentLinkageDiagnosticTests
{
    [Theory]
    [InlineData(WeaveRuntimeMode.Disabled, true, "missing")]
    [InlineData(WeaveRuntimeMode.Legacy, true, "missing")]
    [InlineData(WeaveRuntimeMode.Current, false, "missing")]
    [InlineData(WeaveRuntimeMode.Current, true, null)]
    public void OtherLaunchesRemainIdentical(WeaveRuntimeMode mode, bool capture, string? agent)
    {
        var plan = new LaunchPlan(mode, [], [], [], []);
        Assert.Same(plan, NativeBridgeLauncher.PrepareCurrentLinkageDiagnostic(plan, capture, agent, null));
    }
    [Fact]
    public void DiagnosticPreservesOriginalLoaderAndModAndRequiresEvidence()
    {
        var directory = Path.Combine(Path.GetTempPath(), "sorel-linkage-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var agent = Path.Combine(directory, "diagnostic.jar"); File.WriteAllText(agent, "fixture");
            var original = new LaunchAgent("untouched-loader.jar", null, "loader", LaunchAgentRole.WeaveLoader);
            var plan = new LaunchPlan(WeaveRuntimeMode.Current, ["untouched-mod.jar"], [original], [], []);
            Assert.Throws<InvalidOperationException>(() => NativeBridgeLauncher.PrepareCurrentLinkageDiagnostic(plan, true, agent, "relative"));
            var result = NativeBridgeLauncher.PrepareCurrentLinkageDiagnostic(plan, true, agent, directory);
            Assert.Same(original, result.Agents[0]); Assert.Equal(2, result.Agents.Count);
            Assert.Same(plan.ModPaths, result.ModPaths);
            Assert.Equal(Path.Combine(directory, "current-linkage.log"), result.Agents[1].Options);
            Assert.Single(Directory.GetFiles(directory));
        }
        finally { Directory.Delete(directory, true); }
    }
}
