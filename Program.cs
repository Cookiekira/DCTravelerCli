using DCTravelCli.Commands;
using DCTravelCli.Infrastructure;
using DCTravelCli.Services;
using DCTravelCli.Ux;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddSingleton(AnsiConsole.Console);
services.AddSingleton<ChromeLauncher>();
services.AddSingleton<ISessionStore, FileSessionStore>();
services.AddSingleton<ISessionAcquirer, ChromeSessionAcquirer>();
services.AddSingleton<IWeGameLoginNavigator, WeGameLoginNavigator>();
services.AddSingleton<ITravelApiFactory, OfficialTravelApiFactory>();
services.AddSingleton<ICharacterDiscovery, CharacterDiscovery>();
services.AddSingleton<ITravelPrompts, SpectreTravelPrompts>();
services.AddSingleton<ITravelFlow, TravelFlow>();

var registrar = new DependencyInjectionTypeRegistrar(services);
var app = new CommandApp(registrar);
app.SetDefaultCommand<TravelCommand>();

app.Configure(config =>
{
    config.SetApplicationName("dctravel");
    config.Settings.ShowOptionDefaultValues = true;

    config.AddCommand<TravelCommand>("travel")
        .WithDescription("一键执行 FF14 国服超域传送流程。");

    config.AddCommand<LoginCommand>("login")
        .WithDescription("只刷新登录会话，不提交超域传送订单。");

    config.SetExceptionHandler((exception, _) =>
    {
        AnsiConsole.MarkupLine($"[red]错误：[/] {Markup.Escape(exception.Message)}");
        return 1;
    });
});

return await app.RunAsync(args);
