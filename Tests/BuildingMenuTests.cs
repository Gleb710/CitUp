using CitUp.Sim;
using Xunit;

namespace Tests;

public class BuildingMenuTests
{
    [Fact]
    public void NextRotation_Fisherman_CyclesThroughAllFour()
    {
        var fisherman = BuildingCatalog.LoadDefault().FindBuilding("fisherman")!;

        Assert.Equal(90, fisherman.NextRotation(0));
        Assert.Equal(180, fisherman.NextRotation(90));
        Assert.Equal(270, fisherman.NextRotation(180));
        Assert.Equal(0, fisherman.NextRotation(270));
    }

    [Fact]
    public void NextRotation_House_StaysZero()
    {
        var house = BuildingCatalog.LoadDefault().FindBuilding("house")!;

        Assert.False(house.CanRotate);
        Assert.Equal(0, house.NextRotation(0));
    }

    [Fact]
    public void CanRotate_IsTrueForFishermanAndMine()
    {
        var catalog = BuildingCatalog.LoadDefault();

        Assert.True(catalog.FindBuilding("fisherman")!.CanRotate);
        Assert.True(catalog.FindBuilding("mine")!.CanRotate);
    }

    [Fact]
    public void Fisherman_CanBeBuiltByCommand_WithRotation()
    {
        var world = new World(WorldConfig.LoadDefault());
        var engine = new SimEngine(world);

        engine.Enqueue(new BuildCommand(1, 1, "fisherman", new Cell(10, 10), 90));
        var step = engine.Step();

        Assert.True(Assert.Single(step.Results).Ok);
        Assert.Equal(90, Assert.Single(world.Buildings).Rotation);
    }

    [Fact]
    public void Sawmill_CanBeBuiltByCommand()
    {
        var world = new World(WorldConfig.LoadDefault());
        var engine = new SimEngine(world);

        engine.Enqueue(new BuildCommand(1, 1, "sawmill", new Cell(10, 10)));
        var step = engine.Step();

        Assert.True(Assert.Single(step.Results).Ok);
        Assert.Single(world.Buildings);
    }

    [Fact]
    public void Townhall_CannotBeBuiltByCommand()
    {
        var world = new World(WorldConfig.LoadDefault());
        var engine = new SimEngine(world);

        engine.Enqueue(new BuildCommand(1, 1, "townhall", new Cell(10, 10)));
        var step = engine.Step();

        Assert.Equal(CommandError.UnknownType, Assert.Single(step.Results).Error);
        Assert.Empty(world.Buildings);
    }
}