using System.Reflection;
using DCTravelerCli.Commands;

namespace DCTravelerCli.Tests;

public sealed class CliCommandsTests
{
    [Theory]
    [InlineData(nameof(CliCommands.Run))]
    [InlineData(nameof(CliCommands.Travel))]
    [InlineData(nameof(CliCommands.Login))]
    [InlineData(nameof(CliCommands.Return))]
    public void Session_acquisition_commands_use_browser_neutral_parameters(string methodName)
    {
        var parameterNames = GetParameterNames(methodName);

        Assert.Contains("browserPath", parameterNames);
        Assert.Contains("defaultBrowserProfile", parameterNames);
        Assert.DoesNotContain("chromePath", parameterNames);
        Assert.DoesNotContain("defaultChromeProfile", parameterNames);
    }

    [Fact]
    public void ToSessionOptions_flows_browser_neutral_options()
    {
        var options = CliCommands.ToSessionOptions(
            wegame: true,
            keepBrowserOpen: true,
            verbose: true,
            profileDir: "profile",
            debugPort: 43114,
            loginTimeout: 120,
            defaultBrowserProfile: true,
            browserPath: "browser",
            forceRefresh: true);

        Assert.True(options.UseDefaultBrowserProfile);
        Assert.Equal("browser", options.BrowserPath);
        Assert.True(options.PreferWeGameLogin);
        Assert.True(options.KeepBrowserOpen);
        Assert.True(options.Verbose);
        Assert.True(options.ForceRefresh);
    }

    private static HashSet<string> GetParameterNames(string methodName)
    {
        var method = typeof(CliCommands).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        Assert.NotNull(method);
        return method.GetParameters()
            .Select(parameter => parameter.Name)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
    }
}
