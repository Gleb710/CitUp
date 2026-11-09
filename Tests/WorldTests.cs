using CitUp.Sim;
using Xunit;

namespace Tests;

public class WorldTests
{
    private static World NewWorld() => new(WorldConfig.LoadDefault());

    [Fact]
    public void Config_LoadsFromEmbeddedJson()
    {
        var config = WorldConfig.LoadDefault();

        Assert.Equal(80, config.Map.Width);
        Assert.Equal(100, config.Map.Height);
        Assert.Equal(10, config.TicksPerSecond);
        Assert.Equal(3, config.BuildingTypes.Count);
        Assert.NotNull(config.FindBuildingType("house"));
    }

    [Fact]
    public void NewWorld_HasConfiguredSize_AndTickZero()
    {
        var world = NewWorld();

        Assert.Equal(80, world.Width);
        Assert.Equal(100, world.Height);
        Assert.Equal(0, world.CurrentTick);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(79, 99, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(80, 0, false)]
    [InlineData(0, 100, false)]
    public void IsInside_ChecksMapBounds(int x, int y, bool expected)
    {
        Assert.Equal(expected, NewWorld().IsInside(new Cell(x, y)));
    }

    [Fact]
    public void Step_100Times_GivesTick100_WhichIs10Seconds()
    {
        var world = NewWorld();
        var engine = new SimEngine(world);

        for (int i = 0; i < 100; i++)
            engine.Step();

        Assert.Equal(100, world.CurrentTick);
        Assert.Equal(10, world.CurrentTick / world.Config.TicksPerSecond);
    }

    [Fact]
    public void Step_WithNoCommands_ReturnsNoEvents()
    {
        var engine = new SimEngine(NewWorld());

        Assert.Empty(engine.Step().Events);
    }

    [Fact]
    public void FromJson_BrokenJson_ThrowsClearError()
    {
        var ex = Assert.Throws<InvalidDataException>(() => WorldConfig.FromJson("{ not json"));

        Assert.Contains("JSON", ex.Message);
    }
    
    [Fact]
    public void World_WithoutTerrain_CreatesPlainTerrain()
    {
        var world = new World(WorldConfig.LoadDefault());

        Assert.Equal(world.Width, world.Terrain.Width);
        Assert.Equal(world.Height, world.Terrain.Height);

        for (var y = 0; y < world.Height; y++)
        {
            for (var x = 0; x < world.Width; x++)
            {
                Assert.Equal(
                    TerrainType.Plain,
                    world.Terrain.GetTerrain(new Cell(x, y)));
            }
        }
    }

    [Fact]
    public void World_WithTerrain_UsesProvidedTerrain()
    {
        var terrain = new TerrainGrid(80, 100);
        var cell = new Cell(5, 7);

        terrain.SetTerrain(cell, TerrainType.Water);

        var world = new World(WorldConfig.LoadDefault(), terrain);

        Assert.Equal(TerrainType.Water, world.Terrain.GetTerrain(cell));
    }

    [Fact]
    public void World_WithWrongTerrainSize_Throws()
    {
        var terrain = new TerrainGrid(16, 16);

        Assert.Throws<ArgumentException>(
            () => new World(WorldConfig.LoadDefault(), terrain));
    }
    
    [Fact]
    public void Config_LoadsBuildingSizes()
    {
        var config = WorldConfig.LoadDefault();

        var house = config.FindBuildingType("house");
        var woodcutter = config.FindBuildingType("woodcutter");
        var well = config.FindBuildingType("well");

        Assert.NotNull(house);
        Assert.NotNull(woodcutter);
        Assert.NotNull(well);

        Assert.Equal(2, house!.FootprintWidth);
        Assert.Equal(2, house.FootprintHeight);
        Assert.Equal(4, house.ClearanceWidth);
        Assert.Equal(4, house.ClearanceHeight);

        Assert.Equal(3, woodcutter!.FootprintWidth);
        Assert.Equal(3, woodcutter.FootprintHeight);
        Assert.Equal(5, woodcutter.ClearanceWidth);
        Assert.Equal(5, woodcutter.ClearanceHeight);

        Assert.Equal(2, well!.FootprintWidth);
        Assert.Equal(2, well.FootprintHeight);
        Assert.Equal(4, well.ClearanceWidth);
        Assert.Equal(4, well.ClearanceHeight);
    }

    [Fact]
    public void Config_ClearanceSmallerThanFootprint_Throws()
    {
        var json = """
                   {
                     "ticksPerSecond": 10,
                     "resident": { "ticksPerCell": 5, "pauseTicks": 20 },
                     "buildingTypes": [
                       {
                         "id": "house",
                         "name": "Дом",
                         "color": "#c0504d",
                         "footprintWidth": 3,
                         "footprintHeight": 3,
                         "clearanceWidth": 2,
                         "clearanceHeight": 2
                       }
                     ]
                   }
                   """;

        Assert.Throws<InvalidDataException>(
            () => WorldConfig.FromJson(json));
    }
}