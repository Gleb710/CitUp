using CitUp.Sim;
using Xunit;

namespace CitUp.Tests;

public sealed class WorldStockpileTests
{
    private static (World World, SimEngine Engine) Create()
    {
        var world = new World(WorldConfig.LoadDefault());
        return (world, new SimEngine(world, 1));
    }

    private static BuildCommand Cmd(int id, string type, int x, int y) =>
        new(id, 1, type, new Cell(x, y));

    [Fact]
    public void BuiltBuilding_HasStockpile()
    {
        var (world, engine) = Create();

        engine.Enqueue(Cmd(1, "house", 10, 10));
        engine.Step();

        var building = world.BuildingAt(new Cell(10, 10));

        Assert.NotNull(building);

        var stockpile = world.GetStockpile(building!);
        stockpile.AddResource("wood", 5);

        Assert.Equal(5, stockpile.GetResourceAmount("wood"));
    }

    [Fact]
    public void SameBuilding_ReturnsSameStockpile()
    {
        var (world, engine) = Create();

        engine.Enqueue(Cmd(1, "house", 10, 10));
        engine.Step();

        var building = world.BuildingAt(new Cell(10, 10));
        Assert.NotNull(building);

        var byBuilding = world.GetStockpile(building!);
        var byId = world.GetStockpile(building!.Id);

        Assert.Same(byBuilding, byId);
    }

    [Fact]
    public void UnknownBuilding_Throws()
    {
        var world = new World(WorldConfig.LoadDefault());

        Assert.Throws<InvalidOperationException>(
            () => world.GetStockpile(999));
    }
    
    [Fact]
    public void PlayerStockpile_IsCreatedOnce()
    {
        var world = new World(WorldConfig.LoadDefault());

        var first = world.GetPlayerStockpile(1);
        var second = world.GetPlayerStockpile(1);

        Assert.Same(first, second);
    }

    [Fact]
    public void PlayerStockpile_StoresMoneyAndResourcesSeparately()
    {
        var world = new World(WorldConfig.LoadDefault());
        var stockpile = world.GetPlayerStockpile(1);

        stockpile.AddMoney(1000);
        stockpile.AddResource("wood", 50);

        Assert.Equal(1000, stockpile.Money);
        Assert.Equal(50, stockpile.GetResourceAmount("wood"));
        Assert.Single(stockpile.Resources);
    }

    [Fact]
    public void PlayerStockpile_DifferentOwners_AreSeparate()
    {
        var world = new World(WorldConfig.LoadDefault());

        var first = world.GetPlayerStockpile(1);
        var second = world.GetPlayerStockpile(2);

        Assert.NotSame(first, second);
    }

    [Fact]
    public void PlayerStockpile_InvalidOwner_Throws()
    {
        var world = new World(WorldConfig.LoadDefault());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => world.GetPlayerStockpile(0));
    }
}