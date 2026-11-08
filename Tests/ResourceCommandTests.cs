using CitUp.Sim;
using Xunit;

namespace CitUp.Tests;

public sealed class ResourceCommandTests
{
    private static (World World, SimEngine Engine) Create()
    {
        var world = new World(WorldConfig.LoadDefault());
        return (world, new SimEngine(world, 1));
    }

    private static BuildCommand Cmd(int id, string type, int x, int y) =>
        new(id, 1, type, new Cell(x, y));

    private static AddResourceCommand Resource(int id, int buildingId, string resourceId, int amount) =>
        new(id, 1, buildingId, resourceId, amount);

    [Fact]
    public void AddResourceCommand_AddsResourceToBuildingStockpile()
    {
        var (world, engine) = Create();

        engine.Enqueue(Cmd(1, "house", 10, 10));
        engine.Step();

        var building = world.BuildingAt(new Cell(10, 10));
        Assert.NotNull(building);

        engine.Enqueue(Resource(2, building!.Id, "wood", 7));
        var result = engine.Step();

        Assert.True(result.Results[0].Ok);

        var stockpile = world.GetStockpile(building);
        Assert.Equal(7, stockpile.GetResourceAmount("wood"));
    }

    [Fact]
    public void AddResourceCommand_UnknownBuilding_IsRejected()
    {
        var (world, engine) = Create();

        engine.Enqueue(Resource(1, 999, "wood", 5));
        var result = engine.Step();

        Assert.Equal(
            CommandError.UnknownBuilding,
            result.Results[0].Error);
    }

    [Fact]
    public void AddResourceCommand_NegativeAmount_IsRejected()
    {
        var (world, engine) = Create();

        engine.Enqueue(Cmd(1, "house", 10, 10));
        engine.Step();

        var building = world.BuildingAt(new Cell(10, 10));
        Assert.NotNull(building);

        engine.Enqueue(Resource(2, building!.Id, "wood", -1));
        var result = engine.Step();

        Assert.Equal(
            CommandError.InvalidAmount,
            result.Results[0].Error);
    }
}