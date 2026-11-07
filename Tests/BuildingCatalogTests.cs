using CitUp.Sim;
using Xunit;

namespace Tests;

public class BuildingCatalogTests
{
    [Fact]
    public void LoadDefault_ContainsSevenBuildings()
    {
        var catalog = BuildingCatalog.LoadDefault();

        Assert.Equal(7, catalog.Buildings.Count);

        Assert.NotNull(catalog.FindBuilding("townhall"));
        Assert.NotNull(catalog.FindBuilding("house"));
        Assert.NotNull(catalog.FindBuilding("well"));
        Assert.NotNull(catalog.FindBuilding("woodcutter"));
        Assert.NotNull(catalog.FindBuilding("sawmill"));
        Assert.NotNull(catalog.FindBuilding("fisherman"));
        Assert.NotNull(catalog.FindBuilding("mine"));

        Assert.Null(catalog.FindBuilding("castle"));
    }

    [Fact]
    public void Townhall_HasSpecifiedGeometry()
    {
        var townhall = BuildingCatalog.LoadDefault().FindBuilding("townhall")!;

        Assert.Equal(5, townhall.Width);
        Assert.Equal(7, townhall.Height);

        Assert.Equal(7, townhall.Clearance.Width);
        Assert.Equal(9, townhall.Clearance.Height);

        Assert.Equal(
            BuildingMaskCell.Footprint,
            townhall.Footprint.GetCell(0, 0));

        Assert.Equal(
            BuildingMaskCell.PassableFootprint,
            townhall.Footprint.GetCell(2, 6));

        Assert.Equal(
            BuildingMaskCell.Clearance,
            townhall.Clearance.GetCell(0, 0));

        Assert.Equal(
            BuildingMaskCell.None,
            townhall.Clearance.GetCell(3, 4));
    }

    [Fact]
    public void House_HasSpecifiedGeometry()
    {
        var house = BuildingCatalog.LoadDefault().FindBuilding("house")!;

        Assert.Equal(2, house.Width);
        Assert.Equal(2, house.Height);

        Assert.Equal(4, house.Clearance.Width);
        Assert.Equal(4, house.Clearance.Height);

        Assert.Equal(
            BuildingMaskCell.Footprint,
            house.Footprint.GetCell(1, 1));

        Assert.Equal(
            BuildingMaskCell.Clearance,
            house.Clearance.GetCell(0, 0));

        Assert.Equal(
            BuildingMaskCell.None,
            house.Clearance.GetCell(1, 1));
    }

    [Fact]
    public void FromJson_DuplicateBuildingId_Throws()
    {
        const string json = """
            {
              "buildings": [
                {
                  "id": "house",
                  "name": "Дом",
                  "footprint": ["FF", "FF"],
                  "clearance": ["CCCC", "C..C", "C..C", "CCCC"]
                },
                {
                  "id": "house",
                  "name": "Дом",
                  "footprint": ["FF", "FF"],
                  "clearance": ["CCCC", "C..C", "C..C", "CCCC"]
                }
              ]
            }
            """;

        var exception = Assert.Throws<InvalidDataException>(
            () => BuildingCatalog.FromJson(json));

        Assert.Contains("house", exception.Message);
    }

    [Fact]
    public void FromJson_UnevenFootprintRows_Throws()
    {
        const string json = """
            {
              "buildings": [
                {
                  "id": "house",
                  "name": "Дом",
                  "footprint": ["FF", "F"],
                  "clearance": ["CCCC", "C..C", "C..C", "CCCC"]
                }
              ]
            }
            """;

        var exception = Assert.Throws<InvalidDataException>(
            () => BuildingCatalog.FromJson(json));

        Assert.Contains("footprint", exception.Message);
    }

    [Fact]
    public void FromJson_InvalidMaskCharacter_Throws()
    {
        const string json = """
            {
              "buildings": [
                {
                  "id": "house",
                  "name": "Дом",
                  "footprint": ["FF", "FX"],
                  "clearance": ["CCCC", "C..C", "C..C", "CCCC"]
                }
              ]
            }
            """;

        var exception = Assert.Throws<InvalidDataException>(
            () => BuildingCatalog.FromJson(json));

        Assert.Contains("X", exception.Message);
    }

    [Fact]
    public void FromJson_FootprintWithoutCells_Throws()
    {
        const string json = """
            {
              "buildings": [
                {
                  "id": "house",
                  "name": "Дом",
                  "footprint": ["..", ".."],
                  "clearance": ["CCCC", "C..C", "C..C", "CCCC"]
                }
              ]
            }
            """;

        Assert.Throws<InvalidDataException>(
            () => BuildingCatalog.FromJson(json));
    }

    [Fact]
    public void FromJson_BrokenJson_ThrowsClearError()
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => BuildingCatalog.FromJson("{ not json"));

        Assert.Contains("Data/buildings.json", exception.Message);
    }
    
    [Fact]
    public void Fisherman_HasFourRotations()
    {
        var fisherman = BuildingCatalog.LoadDefault().FindBuilding("fisherman")!;

        Assert.Equal(new[] { 0, 90, 180, 270 }, fisherman.Rotations);
        Assert.Equal(4, fisherman.Footprint.Width);
        Assert.Equal(5, fisherman.Footprint.Height);

        Assert.Equal(
            BuildingMaskCell.Footprint,
            fisherman.Footprint.GetCell(1, 2));

        Assert.Equal(
            BuildingMaskCell.Footprint,
            fisherman.Footprint.GetCell(2, 3));

        Assert.Equal(
            BuildingMaskCell.Access,
            fisherman.Access!.GetCell(0, 0));

        Assert.Equal(
            BuildingMaskCell.Access,
            fisherman.Access.GetCell(3, 0));
    }

    [Fact]
    public void Mine_HasFourRotations()
    {
        var mine = BuildingCatalog.LoadDefault().FindBuilding("mine")!;

        Assert.Equal(new[] { 0, 90, 180, 270 }, mine.Rotations);
        Assert.Equal(4, mine.Footprint.Width);
        Assert.Equal(4, mine.Footprint.Height);

        Assert.Equal(
            BuildingMaskCell.Footprint,
            mine.Footprint.GetCell(1, 2));

        Assert.Equal(
            BuildingMaskCell.Clearance,
            mine.Clearance.GetCell(0, 0));
    }

    [Fact]
    public void FromJson_InvalidRotation_Throws()
    {
        const string json = """
            {
              "buildings": [
                {
                  "id": "house",
                  "name": "Дом",
                  "footprint": ["FF", "FF"],
                  "clearance": ["CCCC", "C..C", "C..C", "CCCC"],
                  "rotations": [45]
                }
              ]
            }
            """;

        var exception = Assert.Throws<InvalidDataException>(
            () => BuildingCatalog.FromJson(json));

        Assert.Contains("45", exception.Message);
    }

    [Fact]
    public void FromJson_DuplicateRotation_Throws()
    {
        const string json = """
            {
              "buildings": [
                {
                  "id": "house",
                  "name": "Дом",
                  "footprint": ["FF", "FF"],
                  "clearance": ["CCCC", "C..C", "C..C", "CCCC"],
                  "rotations": [0, 0]
                }
              ]
            }
            """;

        Assert.Throws<InvalidDataException>(
            () => BuildingCatalog.FromJson(json));
    }
    
    [Fact]
    public void LoadDefault_HasConstructionData()
    {
        var catalog = BuildingCatalog.LoadDefault();

        var house = catalog.FindBuilding("house")!;

        Assert.Equal(180, house.Cost.Money);
        Assert.Equal(2100, house.ConstructionTimeTicks);

        var wood = house.Cost.Resources.Single(resource => resource.ResourceId == "wood");
        var stone = house.Cost.Resources.Single(resource => resource.ResourceId == "stone");

        Assert.Equal(10, wood.Amount);
        Assert.Equal(10, stone.Amount);

        var townhall = catalog.FindBuilding("townhall")!;

        Assert.Equal(0, townhall.Cost.Money);
        Assert.Empty(townhall.Cost.Resources);
        Assert.Equal(0, townhall.ConstructionTimeTicks);
    }

    [Fact]
    public void FromJson_UnknownCostResource_Throws()
    {
        const string json = """
                            {
                              "buildings": [
                                {
                                  "id": "house",
                                  "name": "Дом",
                                  "cost": {
                                    "money": 180,
                                    "resources": {
                                      "gold": 10
                                    }
                                  },
                                  "constructionTimeTicks": 2100,
                                  "footprint": ["FF", "FF"],
                                  "clearance": ["CCCC", "C..C", "C..C", "CCCC"]
                                }
                              ]
                            }
                            """;

        var exception = Assert.Throws<InvalidDataException>(
            () => BuildingCatalog.FromJson(json));

        Assert.Contains("gold", exception.Message);
    }

    [Fact]
    public void FromJson_NegativeConstructionTime_Throws()
    {
        const string json = """
                            {
                              "buildings": [
                                {
                                  "id": "house",
                                  "name": "Дом",
                                  "cost": {
                                    "money": 180,
                                    "resources": {}
                                  },
                                  "constructionTimeTicks": -1,
                                  "footprint": ["FF", "FF"],
                                  "clearance": ["CCCC", "C..C", "C..C", "CCCC"]
                                }
                              ]
                            }
                            """;

        Assert.Throws<InvalidDataException>(
            () => BuildingCatalog.FromJson(json));
    }
}