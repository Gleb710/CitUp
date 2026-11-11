using CitUp.Sim;
using Xunit;

namespace Tests;

public sealed class BuildingPlacementTests
{
    private static (World World, SimEngine Engine) Create()
    {
        var world = new World(WorldConfig.LoadDefault());
        return (world, new SimEngine(world, 1));
    }

    private static BuildCommand Cmd(int id, string type, int x, int y) =>
        new(id, 1, type, new Cell(x, y));

    [Fact]
    public void House_OccupiesAllFootprintCells()
    {
        var (world, engine) = Create();

        engine.Enqueue(Cmd(1, "house", 10, 10));
        engine.Step();

        var house = world.BuildingAt(new Cell(10, 10));
        Assert.NotNull(house);

        Assert.Same(house, world.BuildingAt(new Cell(11, 10)));
        Assert.Same(house, world.BuildingAt(new Cell(10, 11)));
        Assert.Same(house, world.BuildingAt(new Cell(11, 11)));
        Assert.Null(world.BuildingAt(new Cell(12, 10)));
    }

    [Fact]
    public void OverlappingFootprint_IsRejected()
    {
        var (_, engine) = Create();

        engine.Enqueue(Cmd(1, "house", 10, 10));
        engine.Enqueue(Cmd(2, "house", 11, 11));
        var result = engine.Step();

        Assert.True(result.Results[0].Ok);
        Assert.Equal(CommandError.CellOccupied, result.Results[1].Error);
    }

    [Fact]
    public void FootprintPartlyOutsideMap_IsRejected()
    {
        var (world, engine) = Create();

        engine.Enqueue(Cmd(1, "house", world.Width - 1, 10));
        var result = engine.Step();

        Assert.Equal(CommandError.OutOfBounds, result.Results[0].Error);
    }

    [Fact]
    public void Townhall_PassableArea_IsWalkableButOccupied()
    {
        var (world, _) = Create();

        world.Apply(new BuildingBuilt(1, "townhall", new Cell(10, 10), 1));

        Assert.False(GridPathfinder.IsWalkable(world, new Cell(10, 10)));
        Assert.True(GridPathfinder.IsWalkable(world, new Cell(10, 15)));

        Assert.Null(world.BuildingAt(new Cell(10, 15)));
        Assert.NotNull(world.FootprintAt(new Cell(10, 15)));
    }

    [Fact]
    public void Apply_OverlappingBuilding_Throws()
    {
        var (world, _) = Create();

        world.Apply(new BuildingBuilt(1, "house", new Cell(10, 10), 1));

        Assert.Throws<InvalidOperationException>(
            () => world.Apply(new BuildingBuilt(2, "house", new Cell(11, 11), 1)));
    }

    [Fact]
    public void Snapshot_RestoresAllFootprintCells()
    {
        var (world, engine) = Create();

        engine.Enqueue(Cmd(1, "house", 10, 10));
        engine.Step();

        var restored = World.FromSnapshot(world.Config, world.ToSnapshot());

        Assert.NotNull(restored.BuildingAt(new Cell(11, 11)));
        Assert.NotNull(restored.FootprintAt(new Cell(10, 10)));
    }
    
    [Fact]
    public void Snapshot_WithOverlappingBuildings_Throws()
    {
        var config = WorldConfig.LoadDefault();

        var snapshot = new WorldSnapshot(
            0,
            3,
            new[]
            {
                new Building(1, "house", new Cell(10, 10), 1),
                new Building(2, "house", new Cell(11, 11), 1)
            },
            Array.Empty<ResidentSnapshot>());

        Assert.Throws<InvalidDataException>(
            () => World.FromSnapshot(config, snapshot));
    }

    [Fact]
    public void Snapshot_WithFootprintOutsideMap_Throws()
    {
        var config = WorldConfig.LoadDefault();

        var snapshot = new WorldSnapshot(
            0,
            2,
            new[]
            {
                new Building(1, "house", new Cell(config.Map.Width - 1, 10), 1)
            },
            Array.Empty<ResidentSnapshot>());

        Assert.Throws<InvalidDataException>(
            () => World.FromSnapshot(config, snapshot));
    }
}