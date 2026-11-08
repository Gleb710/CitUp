using CitUp.Sim;
using Xunit;

namespace CitUp.Tests;

public sealed class TerrainGridTests
{
    [Fact]
    public void Elevation_Defaults_To_Zero()
    {
        var grid = new TerrainGrid(3, 2);
        var cell = new Cell(1, 1);

        Assert.Equal(0, grid.GetElevation(cell));
    }

    [Fact]
    public void Elevation_Can_Be_Set_And_Read()
    {
        var grid = new TerrainGrid(3, 2);
        var cell = new Cell(2, 0);

        grid.SetElevation(cell, 3);

        Assert.Equal(3, grid.GetElevation(cell));
    }

    [Fact]
    public void Elevation_Cannot_Be_Negative()
    {
        var grid = new TerrainGrid(3, 2);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => grid.SetElevation(new Cell(0, 0), -1));
    }

    [Fact]
    public void Elevation_Cannot_Be_Read_Outside_Map()
    {
        var grid = new TerrainGrid(3, 2);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => grid.GetElevation(new Cell(3, 0)));
    }
}