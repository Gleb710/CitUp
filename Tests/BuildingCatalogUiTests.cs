using CitUp.Sim;
using Xunit;

namespace Tests;

public class BuildingCatalogUiTests
{
    [Fact]
    public void Townhall_IsNotBuildable()
    {
        var catalog = BuildingCatalog.LoadDefault();

        Assert.False(catalog.FindBuilding("townhall")!.Buildable);
    }

    [Fact]
    public void BuildableBuildings_AreInFileOrder_WithoutTownhall()
    {
        var catalog = BuildingCatalog.LoadDefault();

        var ids = catalog.BuildableBuildings.Select(building => building.Id).ToArray();

        Assert.Equal(
            new[] { "house", "well", "woodcutter", "sawmill", "fisherman", "mine" },
            ids);
    }

    [Fact]
    public void AllBuildings_HaveValidColor()
    {
        var catalog = BuildingCatalog.LoadDefault();

        foreach (var building in catalog.Buildings)
            Assert.Matches("^#[0-9a-fA-F]{6}$", building.Color);
    }

    [Fact]
    public void InvalidColor_Throws()
    {
        var json = """
                   {
                     "buildings": [
                       {
                         "id": "test",
                         "name": "Тест",
                         "color": "red",
                         "cost": { "money": 0, "resources": {} },
                         "constructionTimeTicks": 0,
                         "footprint": ["FF", "FF"],
                         "clearance": ["CCCC", "C..C", "C..C", "CCCC"]
                       }
                     ]
                   }
                   """;

        Assert.Throws<InvalidDataException>(() => BuildingCatalog.FromJson(json));
    }
}