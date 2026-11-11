using CitUp.Sim;
using Xunit;

namespace Tests;

public sealed class TerrainPlacementTests
{
    private static (World World, SimEngine Engine, TerrainGrid Terrain) Create()
    {
        var config = WorldConfig.LoadDefault();
        var terrain = new TerrainGrid(config.Map.Width, config.Map.Height);
        var world = new World(config, terrain);
        return (world, new SimEngine(world, 1), terrain);
    }

    private static BuildCommand Cmd(int id, string type, int x, int y) =>
        new(id, 1, type, new Cell(x, y));

    [Theory]
    [InlineData(TerrainType.Forest)]
    [InlineData(TerrainType.Slope)]
    [InlineData(TerrainType.Cliff)]
    [InlineData(TerrainType.ResourceCliff)]
    [InlineData(TerrainType.Water)]
    public void FootprintOnForbiddenTerrain_IsRejected(TerrainType terrainType)
    {
        var (world, engine, terrain) = Create();
        terrain.SetTerrain(new Cell(11, 11), terrainType);

        engine.Enqueue(Cmd(1, "house", 10, 10));
        var result = engine.Step();

        Assert.Equal(CommandError.InvalidTerrain, result.Results[0].Error);
        Assert.Null(world.BuildingAt(new Cell(10, 10)));
    }

    [Theory]
    [InlineData(TerrainType.Forest)]
    [InlineData(TerrainType.Slope)]
    [InlineData(TerrainType.Cliff)]
    [InlineData(TerrainType.ResourceCliff)]
    [InlineData(TerrainType.Water)]
    public void ClearanceOnForbiddenTerrain_IsRejected(TerrainType terrainType)
    {
        var (_, engine, terrain) = Create();
        terrain.SetTerrain(new Cell(9, 9), terrainType);

        engine.Enqueue(Cmd(1, "house", 10, 10));
        var result = engine.Step();

        Assert.Equal(CommandError.InvalidTerrain, result.Results[0].Error);
    }

    [Fact]
    public void FlatPlainArea_IsAllowed()
    {
        var (_, engine, _) = Create();

        engine.Enqueue(Cmd(1, "house", 10, 10));
        var result = engine.Step();

        Assert.True(result.Results[0].Ok);
    }

    [Fact]
    public void FootprintWithDifferentElevations_IsRejected()
    {
        var (_, engine, terrain) = Create();
        terrain.SetElevation(new Cell(11, 11), 1);

        engine.Enqueue(Cmd(1, "house", 10, 10));
        var result = engine.Step();

        Assert.Equal(CommandError.UnevenTerrain, result.Results[0].Error);
    }

    [Fact]
    public void ClearanceWithDifferentElevation_IsRejected()
    {
        var (_, engine, terrain) = Create();
        terrain.SetElevation(new Cell(9, 9), 1);

        engine.Enqueue(Cmd(1, "house", 10, 10));
        var result = engine.Step();

        Assert.Equal(CommandError.UnevenTerrain, result.Results[0].Error);
    }

    [Fact]
    public void RaisedButUniformArea_IsAllowed()
    {
        var (_, engine, terrain) = Create();

        for (var y = 9; y <= 12; y++)
            for (var x = 9; x <= 12; x++)
                terrain.SetElevation(new Cell(x, y), 3);

        engine.Enqueue(Cmd(1, "house", 10, 10));
        var result = engine.Step();

        Assert.True(result.Results[0].Ok);
    }
}