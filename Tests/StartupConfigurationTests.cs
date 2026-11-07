using CitUp.Sim;
using Xunit;

namespace Tests;

public class StartupConfigurationTests
{
    [Fact]
    public void LoadDefault_HasStartingResourcesAndFreeBuildings()
    {
        var startup = StartupConfigurationLoader.LoadDefault();

        Assert.Equal(1000, startup.StartingMoney);

        var wood = Assert.Single(startup.StartingResources);
        Assert.Equal("wood", wood.ResourceId);
        Assert.Equal(50, wood.Amount);

        Assert.Equal(
            new[] { "townhall", "house", "well" },
            startup.FreeBuildingIds);
    }

    [Fact]
    public void FromJson_UnknownStartingResource_Throws()
    {
        const string json = """
            {
              "startingMoney": 1000,
              "startingResources": [
                {
                  "resourceId": "gold",
                  "amount": 50
                }
              ],
              "freeBuildingIds": []
            }
            """;

        var exception = Assert.Throws<InvalidDataException>(
            () => StartupConfigurationLoader.FromJson(json));

        Assert.Contains("gold", exception.Message);
    }

    [Fact]
    public void FromJson_UnknownFreeBuilding_Throws()
    {
        const string json = """
            {
              "startingMoney": 1000,
              "startingResources": [],
              "freeBuildingIds": [
                "castle"
              ]
            }
            """;

        var exception = Assert.Throws<InvalidDataException>(
            () => StartupConfigurationLoader.FromJson(json));

        Assert.Contains("castle", exception.Message);
    }

    [Fact]
    public void FromJson_DuplicateFreeBuilding_Throws()
    {
        const string json = """
            {
              "startingMoney": 1000,
              "startingResources": [],
              "freeBuildingIds": [
                "house",
                "house"
              ]
            }
            """;

        Assert.Throws<InvalidDataException>(
            () => StartupConfigurationLoader.FromJson(json));
    }

    [Fact]
    public void FromJson_NegativeStartingMoney_Throws()
    {
        const string json = """
            {
              "startingMoney": -1,
              "startingResources": [],
              "freeBuildingIds": []
            }
            """;

        Assert.Throws<InvalidDataException>(
            () => StartupConfigurationLoader.FromJson(json));
    }

    [Fact]
    public void FromJson_BrokenJson_ThrowsClearError()
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => StartupConfigurationLoader.FromJson("{ not json"));

        Assert.Contains("Data/startup.json", exception.Message);
    }
}