using DCTravelCli.Domain;
using Spectre.Console;

namespace DCTravelCli.Ux;

public sealed class SpectreTravelPrompts(IAnsiConsole console) : ITravelPrompts
{
    public CharacterSelection SelectCharacter(IReadOnlyList<CharacterSelection> characters)
    {
        return console.Prompt(
            new SelectionPrompt<CharacterSelection>()
                .Title("选择角色")
                .PageSize(10)
                .EnableSearch()
                .SearchPlaceholderText("搜索角色/区服...")
                .MoreChoicesText("[grey](上下移动查看更多角色)[/]")
                .UseConverter(FormatCharacterSelection)
                .AddChoices(characters));
    }

    public ActiveTravelOrder SelectReturnOrder(IReadOnlyList<ActiveTravelOrder> orders)
    {
        return console.Prompt(
            new SelectionPrompt<ActiveTravelOrder>()
                .Title("选择要返回原服的角色")
                .PageSize(10)
                .EnableSearch()
                .SearchPlaceholderText("搜索角色/区服...")
                .MoreChoicesText("[grey](上下移动查看更多订单)[/]")
                .UseConverter(order =>
                    $"{Markup.Escape(order.RoleName)} {FormatRoleId(order.RoleId)} " +
                    $"[blue]当前：{Markup.Escape(order.CurrentRegion.AreaName)} / {Markup.Escape(order.CurrentWorld.GroupName)}[/] " +
                    $"[grey]原服：{Markup.Escape(order.HomeRegion.AreaName)} / {Markup.Escape(order.HomeWorld.GroupName)}[/]")
                .AddChoices(orders));
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

    public bool ConfirmReturn(ActiveTravelOrder order, bool assumeYes)
    {
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .HideHeaders()
            .AddColumn("Field")
            .AddColumn("Value")
            .AddRow("角色", $"{Markup.Escape(order.RoleName)} {FormatRoleId(order.RoleId)}")
            .AddRow("从", $"{Markup.Escape(order.CurrentRegion.AreaName)} / {Markup.Escape(order.CurrentWorld.GroupName)}")
            .AddRow("回到", $"{Markup.Escape(order.HomeRegion.AreaName)} / {Markup.Escape(order.HomeWorld.GroupName)}")
            .AddRow("旅行订单", Markup.Escape(order.OrderId));

        console.Write(new Panel(table)
            .Header("即将提交超域返回")
            .BorderColor(Color.Cyan1));

        return assumeYes ||
            console.Confirm("确认执行超域返回？", defaultValue: false);
    }

    public bool ConfirmReturnThenTravel(ReturnThenTravelSelection selection, bool assumeYes)
    {
        var order = selection.Order;
        var target = selection.Target;
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .HideHeaders()
            .AddColumn("Field")
            .AddColumn("Value")
            .AddRow("角色", $"{Markup.Escape(order.RoleName)} {FormatRoleId(order.RoleId)}")
            .AddRow("先返回", $"{Markup.Escape(order.CurrentRegion.AreaName)} / {Markup.Escape(order.CurrentWorld.GroupName)} → {Markup.Escape(order.HomeRegion.AreaName)} / {Markup.Escape(order.HomeWorld.GroupName)}")
            .AddRow("再传送", $"{Markup.Escape(order.HomeRegion.AreaName)} / {Markup.Escape(order.HomeWorld.GroupName)} → {Markup.Escape(target.Region.AreaName)} / {Markup.Escape(target.World.GroupName)}");

        console.Write(new Panel(table)
            .Header("即将先返回原服再提交超域传送")
            .BorderColor(Color.Cyan1));

        return assumeYes ||
            console.Confirm("确认执行返回后传送？", defaultValue: false);
    }

    public bool ConfirmOfficialSecondStep()
    {
        console.WriteLine();
        console.MarkupLine("[yellow]官网要求二次确认。[/]");
        return console.Confirm("继续本次超域传送？选择否会调用官网放弃接口。", defaultValue: false);
    }

    private static string FormatCharacterSelection(CharacterSelection selection)
    {
        var roleId = FormatRoleId(selection.RoleId);
        if (!selection.RequiresReturnHome)
        {
            return $"{Markup.Escape(selection.RoleName)} {roleId} " +
                $"[blue]{Markup.Escape(selection.HomeRegion.AreaName)} / {Markup.Escape(selection.HomeWorld.GroupName)}[/]";
        }

        return $"{Markup.Escape(selection.RoleName)} {roleId} " +
            $"[yellow]当前：{Markup.Escape(selection.CurrentRegion.AreaName)} / {Markup.Escape(selection.CurrentWorld.GroupName)}[/] " +
            $"[grey]原服：{Markup.Escape(selection.HomeRegion.AreaName)} / {Markup.Escape(selection.HomeWorld.GroupName)}[/]";
    }

    private static string FormatRoleId(string? roleId)
    {
        return string.IsNullOrWhiteSpace(roleId)
            ? "[grey](无角色ID)[/]"
            : $"[grey]({Markup.Escape(roleId)})[/]";
    }
}
