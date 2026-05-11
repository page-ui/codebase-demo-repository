using Page.Ui.SvelteRender.Services;

namespace Page.Ui.Backend.Tests.SvelteRender;

public class RenderRunPathTests
{
    [Fact]
    public void GetPhysicalRunPath_KeepsMetadataSegmentsWithinRunsDirectory()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var metadata = new Dictionary<string, string>
        {
            ["userStorageKey"] = "../../etc/passwd",
            ["chatKey"] = "..\\..\\other",
            ["versionId"] = "../version"
        };

        var path = RenderRunPath.GetPhysicalRunPath(contentRoot, "runs", metadata, "run-1");
        var runsRoot = Path.GetFullPath(Path.Combine(contentRoot, "runs"));
        var relativePath = Path.GetRelativePath(runsRoot, path);

        Assert.False(Path.IsPathRooted(relativePath));
        Assert.False(relativePath.StartsWith("..", StringComparison.Ordinal));
        Assert.DoesNotContain($"{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}", path);
    }

    [Fact]
    public void GetContainedRootPath_RejectsSiblingPrefixConfusion()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), "render-root");
        var sibling = contentRoot + "-sibling";

        Assert.Throws<InvalidOperationException>(() =>
            RenderPathGuard.GetContainedRootPath(contentRoot, sibling));
    }
}
