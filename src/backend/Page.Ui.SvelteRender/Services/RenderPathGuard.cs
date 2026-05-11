namespace Page.Ui.SvelteRender.Services;

internal static class RenderPathGuard
{
    public static string GetContainedRootPath(string contentRootPath, string runsDirectory)
    {
        var contentRoot = Path.GetFullPath(contentRootPath);
        var runsRoot = Path.GetFullPath(Path.Combine(contentRoot, runsDirectory));
        if (!IsPathWithinRoot(runsRoot, contentRoot))
        {
            throw new InvalidOperationException("RenderOptions.RunsDirectory must stay within the content root.");
        }

        return runsRoot;
    }

    public static string GetContainedPath(string rootPath, params string[] segments)
    {
        var root = EnsureTrailingSeparator(Path.GetFullPath(rootPath));
        var combined = Path.GetFullPath(Path.Combine(new[] { rootPath }.Concat(segments).ToArray()));
        if (!IsPathWithinRoot(combined, root))
        {
            throw new InvalidOperationException("Render path must stay within the configured runs directory.");
        }

        return combined;
    }

    private static bool IsPathWithinRoot(string candidatePath, string rootPath)
    {
        var relativePath = Path.GetRelativePath(rootPath, candidatePath);
        return relativePath == "." ||
               (!relativePath.StartsWith("..", StringComparison.Ordinal) &&
                !Path.IsPathRooted(relativePath));
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
