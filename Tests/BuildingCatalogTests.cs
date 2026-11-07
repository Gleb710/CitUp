using CitUp.Sim;
using Xunit;

namespace Tests;

public class BuildingCatalogTests
{
    [Fact]
    public void LoadDefault_ContainsFiveBuildings()
    {
        var catalog = BuildingCatalog.LoadDefault();

        Assert.Equal(5, catalog.Buildings.Count);

        Assert.NotNull(catalog.FindBuilding("townhall"));
        Assert.NotNull(catalog.FindBuilding("house"));
        Assert.NotNull(catalog.FindBuilding("well"));
        Assert.NotNull(catalog.FindBuilding("woodcutter"));
        Assert.NotNull(catalog.FindBuilding("sawmill"));

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
}