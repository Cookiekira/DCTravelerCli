using ConsoleAppFramework;
using DCTravelerCli.Commands;
using DCTravelerCli.Infrastructure;
using DCTravelerCli.Services;
using DCTravelerCli.Ux;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

ConsoleApp.LogError = message =>
    AnsiConsole.MarkupLine($"[red]错误：[/] {Markup.Escape(message)}");

var app = ConsoleApp.Create()
    .ConfigureServices(services =>
    {
        services.AddSingleton(AnsiConsole.Console);
        services.AddSingleton<BrowserDiscovery>();
        services.AddSingleton<BrowserLauncher>();
        services.AddSingleton<ISessionStore, FileSessionStore>();
        services.AddSingleton<ISessionAcquirer, BrowserSessionAcquirer>();
        services.AddSingleton<IWeGameLoginNavigator, WeGameLoginNavigator>();
        services.AddSingleton<ITravelApiFactory, OfficialTravelApiFactory>();
        services.AddSingleton<ICharacterDiscovery, CharacterDiscovery>();
        services.AddSingleton<ICharacterSelectionCatalogBuilder, CharacterSelectionCatalogBuilder>();
        services.AddSingleton<IReturnHomeService, ReturnHomeService>();
        services.AddSingleton<ITravelPrompts, SpectreTravelPrompts>();
        services.AddSingleton<ITravelFlow, TravelFlow>();
        services.AddSingleton<IReturnFlow, ReturnFlow>();
    });

app.UseFilter<CliExceptionFilter>();
app.Add<CliCommands>();

await app.RunAsync(NormalizeHelpArgs(args));
return Environment.ExitCode;

static string[] NormalizeHelpArgs(string[] args)
{
    if (!args.Contains("-h", StringComparer.Ordinal) &&
        !args.Contains("--help", StringComparer.Ordinal))
    {
        return args;
    }

    var command = args.FirstOrDefault(arg =>
        string.Equals(arg, "travel", StringComparison.Ordinal) ||
        string.Equals(arg, "login", StringComparison.Ordinal) ||
        string.Equals(arg, "return", StringComparison.Ordinal));

    return command is null ? ["--help"] : [command, "--help"];
}
