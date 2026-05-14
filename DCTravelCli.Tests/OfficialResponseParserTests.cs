using System.Text.Json;
using DCTravelCli.Domain;
using DCTravelCli.Services;

namespace DCTravelCli.Tests;

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
    public void ToCharacters_preserves_official_payload_and_adds_key()
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
        Assert.Equal("PLD", characters[0].OfficialPayload["job"]!.GetValue<string>());
        Assert.Equal(0, characters[0].OfficialPayload["key"]!.GetValue<int>());
    }
}
