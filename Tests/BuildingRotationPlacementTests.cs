using CitUp.Sim;
using Xunit;

namespace Tests;

public sealed class BuildingRotationPlacementTests
{
    private static World NewWorld() => new(WorldConfig.LoadDefault());

    [Fact]
    public void BuildCommand_DefaultRotationIsZero() =>
        Assert.Equal(0, new BuildCommand(1, 1, "house", new Cell(10, 10)).Rotation);

    [Fact]
    public void UnsupportedRotation_IsRejected()
    {
        var world = NewWorld();
        var engine = new SimEngine(world, 1);

        engine.Enqueue(new BuildCommand(1, 1, "house", new Cell(10, 10), 90));
        var result = engine.Step();

        Assert.Equal(CommandError.InvalidRotation, result.Results[0].Error);
        Assert.Null(world.BuildingAt(new Cell(10, 10)));
    }

    [Fact]
    public void BuiltBuilding_StoresRotation()
    {
        var world = NewWorld();
        var engine = new SimEngine(world, 1);

        engine.Enqueue(new BuildCommand(1, 1, "house", new Cell(10, 10)));
        engine.Step();

        Assert.Equal(0, world.BuildingAt(new Cell(10, 10))!.Rotation);
    }

    [Fact]
    public void Fisherman_Rotation0_OccupiesLowerPart()
    {
        var world = NewWorld();

        world.Apply(new BuildingBuilt(1, "fisherman", new Cell(10, 10), 1, 0));

        Assert.NotNull(world.BuildingAt(new Cell(11, 13)));
        Assert.Null(world.BuildingAt(new Cell(11, 11)));
    }

    [Fact]
    public void Fisherman_Rotation90_OccupiesUpperPart()
    {
        var world = NewWorld();

        world.Apply(new BuildingBuilt(1, "fisherman", new Cell(10, 10), 1, 90));

        Assert.NotNull(world.BuildingAt(new Cell(11, 11)));
        Assert.NotNull(world.BuildingAt(new Cell(12, 12)));
        Assert.Null(world.BuildingAt(new Cell(10, 10)));
    }

    [Fact]
    public void Apply_UnsupportedRotation_Throws()
    {
        var world = NewWorld();

        Assert.Throws<InvalidOperationException>(
            () => world.Apply(new BuildingBuilt(1, "house", new Cell(10, 10), 1, 90)));
    }

    [Fact]
    public void Snapshot_PreservesRotation()
    {
        var world = NewWorld();
        world.Apply(new BuildingBuilt(1, "fisherman", new Cell(10, 10), 1, 90));

        var restored = World.FromSnapshot(world.Config, world.ToSnapshot());

        Assert.Equal(90, restored.FindBuilding(1)!.Rotation);
        Assert.NotNull(restored.BuildingAt(new Cell(11, 11)));
    }

    [Fact]
    public void StateHash_DependsOnRotation()
    {
        var first = NewWorld();
        var second = NewWorld();

        first.Apply(new BuildingBuilt(1, "fisherman", new Cell(10, 10), 1, 0));
        second.Apply(new BuildingBuilt(1, "fisherman", new Cell(10, 10), 1, 90));

        Assert.NotEqual(first.StateHash(), second.StateHash());
    }
}