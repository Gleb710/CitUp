using CitUp.Sim;
using Xunit;

namespace CitUp.Tests;

public sealed class WorldGeometryTests
{
    [Fact]
    public void BuiltHouse_ReturnsFootprintAndClearance()
    {
        var world = new World(WorldConfig.LoadDefault());
        var engine = new SimEngine(world, 1);

        engine.Enqueue(new BuildCommand(1, 1, "house", new Cell(10, 10)));
        engine.Step();

        var house = world.BuildingAt(new Cell(10, 10));
        Assert.NotNull(house);

        var footprint = world.GetFootprintCells(house!);
        var clearance = world.GetClearanceCells(house!);

        Assert.Equal(4, footprint.Count);
        Assert.Contains(new Cell(11, 11), footprint);

        Assert.Equal(12, clearance.Count);
        Assert.Contains(new Cell(9, 9), clearance);
        Assert.Contains(new Cell(12, 12), clearance);
    }

    [Fact]
    public void UnknownBuildingType_Throws()
    {
        var world = new World(WorldConfig.LoadDefault());
        var building = new Building(99, "unknown", new Cell(1, 1), 1);

        Assert.Throws<InvalidOperationException>(
            () => world.GetFootprintCells(building));
    }

    [Fact]
    public void World_UsesProvidedBuildingCatalog()
    {
        var catalog = BuildingCatalog.LoadDefault();
        var config = WorldConfig.LoadDefault();
        var terrain = new TerrainGrid(config.Map.Width, config.Map.Height);

        var world = new World(config, terrain, catalog);

        Assert.Same(catalog, world.BuildingCatalog);
    }
}