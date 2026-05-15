using System.Text.Json;
using DCTravelerCli.Domain;
using DCTravelerCli.Services;

namespace DCTravelerCli.Tests;

public sealed class OfficialResponseParserTests
{
    [Fact]
    public void ToSourceRegions_reads_group_list_from_array()
    {
        using var document = JsonDocument.Parse(
            """
            [
              {
                "areaId": 1,
                "areaName": "陆行鸟",
                "groups": [
                  { "groupId": 10, "groupCode": "A", "groupName": "红玉海" }
                ]
              }
            ]
            """);

        var regions = OfficialResponseParser.ToSourceRegions(document.RootElement);

        Assert.Single(regions);
        Assert.Equal("陆行鸟", regions[0].AreaName);
        Assert.Equal("红玉海", regions[0].Worlds[0].GroupName);
    }

    [Fact]
    public void ToSourceRegions_reads_group_list_from_json_string()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "groupList": "[{\"areaId\":2,\"areaName\":\"莫古力\",\"groups\":[{\"groupId\":20,\"groupCode\":\"B\",\"groupName\":\"神意之地\"}]}]"
            }
            """);

        var regions = OfficialResponseParser.ToSourceRegions(document.RootElement.GetProperty("groupList"));

        Assert.Single(regions);
        Assert.Equal("莫古力", regions[0].AreaName);
        Assert.Equal("神意之地", regions[0].Worlds[0].GroupName);
    }

    [Fact]
    public void ToCharacters_reads_character_facts()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "roleList": "[{\"roleId\":\"100\",\"roleName\":\"Cookie\",\"job\":\"PLD\"}]"
            }
            """);
        var region = new SourceRegion(1, "陆行鸟", []);
        var world = new SourceWorld(10, "A", "红玉海");

        var characters = OfficialResponseParser.ToCharacters(
            document.RootElement.GetProperty("roleList"),
            region,
            world);

        Assert.Single(characters);
        Assert.Equal("100", characters[0].RoleId);
        Assert.Equal("Cookie", characters[0].RoleName);
        Assert.Equal("红玉海", characters[0].SourceWorld.GroupName);
    }

    [Fact]
    public void ToOfficialCharacters_preserves_submission_payload_and_adds_key()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "roleList": "[{\"roleId\":\"100\",\"roleName\":\"Cookie\",\"job\":\"PLD\"}]"
            }
            """);
        var region = new SourceRegion(1, "陆行鸟", []);
        var world = new SourceWorld(10, "A", "红玉海");

        var characters = OfficialResponseParser.ToOfficialCharacters(
            document.RootElement.GetProperty("roleList"),
            region,
            world);

        Assert.Single(characters);
        Assert.Equal("100", characters[0].Character.RoleId);
        Assert.Equal("PLD", characters[0].SubmissionPayload["job"]!.GetValue<string>());
        Assert.Equal(0, characters[0].SubmissionPayload["key"]!.GetValue<int>());
    }

    [Fact]
    public void ToActiveTravelOrders_reads_active_order_with_string_detail_list()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "orderlist": "[{\"orderId\":\"T-1\",\"migrationType\":4,\"migrationStatus\":5,\"travelStatus\":1,\"migrationStatusDesc\":\"旅行中【已达到目的地】\",\"areaId\":1,\"areaName\":\"陆行鸟\",\"groupId\":10,\"groupCode\":\"A\",\"groupName\":\"红玉海\",\"targetAreaId\":2,\"targetAreaName\":\"莫古力\",\"targetGroupId\":20,\"targetGroupCode\":\"B\",\"targetGroupName\":\"神意之地\",\"migrationDetailList\":\"[{\\\"roleId\\\":\\\"100\\\",\\\"roleName\\\":\\\"Cookie\\\"}]\"}]"
            }
            """);

        var elements = OfficialResponseParser.ReadElements(document.RootElement.GetProperty("orderlist"));
        var orders = OfficialResponseParser.ToActiveTravelOrders(elements);

        Assert.Single(orders);
        Assert.Equal("T-1", orders[0].OrderId);
        Assert.Equal("100", orders[0].RoleId);
        Assert.Equal("Cookie", orders[0].RoleName);
        Assert.Equal("红玉海", orders[0].HomeWorld.GroupName);
        Assert.Equal("神意之地", orders[0].CurrentWorld.GroupName);
    }

    [Fact]
    public void ToActiveTravelOrders_ignores_ended_and_return_orders()
    {
        using var document = JsonDocument.Parse(
            """
            [
              { "orderId": "ended", "migrationType": 4, "migrationStatus": 5, "travelStatus": 3, "migrationStatusDesc": "旅行结束" },
              { "orderId": "return", "migrationType": 5, "migrationStatus": 5, "travelStatus": 1, "migrationStatusDesc": "返回成功" }
            ]
            """);

        var orders = OfficialResponseParser.ToActiveTravelOrders(
            OfficialResponseParser.ReadElements(document.RootElement));

        Assert.Empty(orders);
    }

    [Fact]
    public void ToActiveTravelOrders_collapses_duplicate_active_orders_for_same_role()
    {
        using var document = JsonDocument.Parse(
            """
            [
              { "orderId": "new", "migrationType": 4, "migrationStatus": 5, "travelStatus": 1, "migrationStatusDesc": "旅行中", "areaId": 1, "areaName": "莫古力", "groupId": 10, "groupCode": "A", "groupName": "白银乡", "targetAreaId": 2, "targetAreaName": "陆行鸟", "targetGroupId": 22, "targetGroupCode": "new", "targetGroupName": "红玉海", "migrationDetailList": [{ "roleId": "100", "roleName": "斜膀泰迪" }] },
              { "orderId": "old", "migrationType": 4, "migrationStatus": 5, "travelStatus": 1, "migrationStatusDesc": "旅行中", "areaId": 1, "areaName": "莫古力", "groupId": 10, "groupCode": "A", "groupName": "白银乡", "targetAreaId": 2, "targetAreaName": "陆行鸟", "targetGroupId": 21, "targetGroupCode": "old", "targetGroupName": "萌芽池", "migrationDetailList": [{ "roleId": "100", "roleName": "斜膀泰迪" }] }
            ]
            """);

        var orders = OfficialResponseParser.ToActiveTravelOrders(
            OfficialResponseParser.ReadElements(document.RootElement));

        Assert.Single(orders);
        Assert.Equal("new", orders[0].OrderId);
        Assert.Equal("红玉海", orders[0].CurrentWorld.GroupName);
    }

    [Fact]
    public void ToMigrationOrders_reads_return_status()
    {
        using var document = JsonDocument.Parse(
            """
            [
              { "orderId": "R-1", "migrationType": 5, "migrationStatus": 5, "travelStatus": 0, "migrationStatusDesc": "返回成功" }
            ]
            """);

        var orders = OfficialResponseParser.ToMigrationOrders(
            OfficialResponseParser.ReadElements(document.RootElement));

        Assert.Single(orders);
        Assert.Equal(5, orders[0].MigrationType);
        Assert.Equal("返回成功", orders[0].StatusDescription);
    }
}
