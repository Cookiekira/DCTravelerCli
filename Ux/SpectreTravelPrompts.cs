using DCTravelCli.Domain;
using Spectre.Console;

namespace DCTravelCli.Ux;

public sealed class SpectreTravelPrompts(IAnsiConsole console) : ITravelPrompts
{
    public Character SelectCharacter(IReadOnlyList<Character> characters)
    {
        return console.Prompt(
            new SelectionPrompt<Character>()
                .Title("选择角色")
                .PageSize(10)
                .EnableSearch()
                .SearchPlaceholderText("搜索角色/区服...")
                .MoreChoicesText("[grey](上下移动查看更多角色)[/]")
                .UseConverter(character =>
                    $"{Markup.Escape(character.RoleName)} [grey]({Markup.Escape(character.RoleId)})[/] " +
                    $"[blue]{Markup.Escape(character.SourceRegion.AreaName)} / {Markup.Escape(character.SourceWorld.GroupName)}[/]")
                .AddChoices(characters));
    }

    public TargetRegion SelectTargetRegion(IReadOnlyList<TargetRegion> regions)
    {
        return console.Prompt(
            new SelectionPrompt<TargetRegion>()
                .Title("选择目标大区")
                .PageSize(8)
                .EnableSearch()
                .SearchPlaceholderText("搜索目标大区...")
                .UseConverter(region => Markup.Escape(region.AreaName))
                .AddChoices(regions));
    }

    public TargetWorld SelectTargetWorld(TargetRegion region)
    {
        return console.Prompt(
            new SelectionPrompt<TargetWorld>()
                .Title($"选择 [green]{Markup.Escape(region.AreaName)}[/] 的目标服务器")
                .PageSize(12)
                .EnableSearch()
                .SearchPlaceholderText("搜索目标服务器...")
                .UseConverter(world => Markup.Escape(world.GroupName))
                .AddChoices(region.Worlds));
    }

    public bool ConfirmOrder(TravelSelection selection, bool assumeYes)
    {
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .HideHeaders()
            .AddColumn("Field")
            .AddColumn("Value")
            .AddRow("角色", $"{Markup.Escape(selection.Character.RoleName)} [grey]({Markup.Escape(selection.Character.RoleId)})[/]")
            .AddRow("从", $"{Markup.Escape(selection.Character.SourceRegion.AreaName)} / {Markup.Escape(selection.Character.SourceWorld.GroupName)}")
            .AddRow("到", $"{Markup.Escape(selection.Target.Region.AreaName)} / {Markup.Escape(selection.Target.World.GroupName)}");

        console.Write(new Panel(table)
            .Header("即将提交超域传送订单")
            .BorderColor(Color.Cyan1));

        return assumeYes ||
            console.Confirm("确认提交超域传送订单？", defaultValue: false);
    }

    public bool ConfirmOfficialSecondStep()
    {
        console.WriteLine();
        console.MarkupLine("[yellow]官网要求二次确认。[/]");
        return console.Confirm("继续本次超域传送？选择否会调用官网放弃接口。", defaultValue: false);
    }
}
