using System.Runtime.CompilerServices;

namespace TopekaIT.Web.Tests;

internal static class RepositorySource
{
    public static string Read(params string[] relativePath) =>
        File.ReadAllText(Path(relativePath));

    public static string Path(params string[] relativePath) =>
        Path(relativePath, SourceFile());

    private static string SourceFile([CallerFilePath] string sourceFile = "") => sourceFile;

    private static string Path(string[] relativePath, string sourceFile)
    {
        foreach (var start in CandidateStarts(sourceFile))
        {
            var dir = new DirectoryInfo(start);
            while (dir != null && !File.Exists(System.IO.Path.Combine(dir.FullName, "6IA-IT-Portal.slnx")))
            {
                dir = dir.Parent;
            }

            if (dir != null)
            {
                return System.IO.Path.Combine(new[] { dir.FullName }.Concat(relativePath).ToArray());
            }
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static IEnumerable<string> CandidateStarts(string sourceFile)
    {
        var sourceDirectory = System.IO.Path.GetDirectoryName(sourceFile);
        if (!string.IsNullOrWhiteSpace(sourceDirectory))
        {
            yield return sourceDirectory;
        }

        yield return Directory.GetCurrentDirectory();
        yield return AppContext.BaseDirectory;
    }
}
