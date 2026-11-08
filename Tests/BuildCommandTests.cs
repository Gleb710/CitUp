using CitUp.Sim;
using Xunit;

namespace Tests;

public class BuildCommandTests
{
    private static (World World, SimEngine Engine) Create()
    {
        var world = new World(WorldConfig.LoadDefault());
        return (world, new SimEngine(world));
    }

    private static BuildCommand Cmd(int id, string type, int x, int y, int player = 1) =>
        new(id, player, type, new Cell(x, y));

    [Fact]
    public void Build_OnFreeCell_Succeeds()
    {
        var (world, engine) = Create();

        engine.Enqueue(Cmd(1, "house", 5, 7));
        var result = engine.Step();

        var commandResult = Assert.Single(result.Results);
        Assert.True(commandResult.Ok);
        Assert.Equal(1, commandResult.CommandId);

        var built = Assert.IsType<BuildingBuilt>(Assert.Single(result.Events));
        Assert.Equal("house", built.Type);
        Assert.Equal(new Cell(5, 7), built.Cell);

        var building = Assert.Single(world.Buildings);
        Assert.Equal(building, world.BuildingAt(new Cell(5, 7)));
    }

    [Fact]
    public void Build_OnOccupiedCell_IsRejected_AndWorldUnchanged()
    {
        var (world, engine) = Create();
        engine.Enqueue(Cmd(1, "house", 5, 7));
        engine.Step();

        engine.Enqueue(Cmd(2, "well", 5, 7));
        var result = engine.Step();

        Assert.Equal(CommandError.CellOccupied, Assert.Single(result.Results).Error);
        Assert.Empty(result.Events);
        Assert.Single(world.Buildings);
        Assert.Equal("house", world.BuildingAt(new Cell(5, 7))!.Type);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(80, 0)]
    [InlineData(0, 100)]
    public void Build_OutsideMap_IsRejected(int x, int y)
    {
        var (world, engine) = Create();

        engine.Enqueue(Cmd(1, "house", x, y));
        var result = engine.Step();

        Assert.Equal(CommandError.OutOfBounds, Assert.Single(result.Results).Error);
        Assert.Empty(world.Buildings);
    }

    [Fact]
    public void Build_UnknownType_IsRejected()
    {
        var (world, engine) = Create();

        engine.Enqueue(Cmd(1, "castle", 3, 3));
        var result = engine.Step();

        Assert.Equal(CommandError.UnknownType, Assert.Single(result.Results).Error);
        Assert.Empty(world.Buildings);
    }

    [Fact]
    public void TwoCommandsOnSameCell_InOneTick_FirstWins()
    {
        var (world, engine) = Create();

        engine.Enqueue(Cmd(1, "house", 4, 4, player: 1));
        engine.Enqueue(Cmd(2, "well", 4, 4, player: 2));
        var result = engine.Step();

        Assert.True(result.Results[0].Ok);
        Assert.Equal(CommandError.CellOccupied, result.Results[1].Error);
        Assert.Single(result.Events);

        var building = Assert.Single(world.Buildings);
        Assert.Equal("house", building.Type);
        Assert.Equal(1, building.OwnerId);
    }

    [Fact]
    public void BuildingIds_GrowFromOne()
    {
        var (world, engine) = Create();

        engine.Enqueue(Cmd(1, "house", 1, 1));
        engine.Enqueue(Cmd(2, "well", 2, 2));
        engine.Step();

        Assert.Equal(new[] { 1, 2 }, world.Buildings.Select(b => b.Id).ToArray());
        Assert.Equal(3, world.NextBuildingId);
    }

    [Fact]
    public void Apply_OnCleanWorld_ReproducesEngineState()
    {
        var (hostWorld, engine) = Create();
        var allEvents = new List<SimEvent>();

        engine.Enqueue(Cmd(1, "house", 5, 7, player: 1));
        engine.Enqueue(Cmd(2, "well", 8, 2, player: 2));
        allEvents.AddRange(engine.Step().Events);
        engine.Enqueue(Cmd(3, "woodcutter", 10, 10, player: 1));
        allEvents.AddRange(engine.Step().Events);

        var clientWorld = new World(WorldConfig.LoadDefault());
        foreach (var simEvent in allEvents)
            clientWorld.Apply(simEvent);

        Assert.Equal(hostWorld.Buildings, clientWorld.Buildings);
        Assert.Equal(hostWorld.NextBuildingId, clientWorld.NextBuildingId);
    }

    [Fact]
    public void Apply_BuildingOnOccupiedCell_Throws()
    {
        var world = new World(WorldConfig.LoadDefault());
        var cell = new Cell(2, 2);
        world.Apply(new BuildingBuilt(1, "house", cell, 1));

        Assert.Throws<InvalidOperationException>(() =>
            world.Apply(new BuildingBuilt(2, "well", cell, 1)));
    }
}