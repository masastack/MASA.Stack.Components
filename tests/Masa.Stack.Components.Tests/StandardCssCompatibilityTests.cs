using Xunit;

namespace Masa.Stack.Components.Tests;

public class StandardCssCompatibilityTests
{
    private static readonly string[] ColorUtilities =
    [
        "default",
        "emphasis",
        "emphasis2",
        "regular",
        "regular2",
        "regular3",
        "fill"
    ];

    [Fact]
    public void SharedColorUtilitiesKeepTheirThemeTokenContract()
    {
        var css = File.ReadAllText(FindRepositoryFile(
            "src", "Masa.Stack.Components", "wwwroot", "css", "standard.css"));

        foreach (var utility in ColorUtilities)
        {
            Assert.Contains($".masa.m-application .{utility} {{", css);
            Assert.Contains($"var(--ms-background-{utility},", css);
            Assert.Contains($".masa.m-application .{utility}--text {{", css);
            Assert.Contains($"var(--ms-text-{utility},", css);
        }
    }

    private static string FindRepositoryFile(params string[] pathParts)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var path = Path.Combine([directory.FullName, .. pathParts]);
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException("Could not locate standard.css from the test output directory.");
    }
}
