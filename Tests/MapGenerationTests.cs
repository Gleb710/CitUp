using CitUp.Sim;
using Xunit;

namespace Tests;

public class MapGenerationTests
{
    [Fact]
    public void NewTerrainGrid_IsFilledWithPlain()
    {
        var grid = new TerrainGrid(4, 3);

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                Assert.Equal(
                    TerrainType.Plain,
                    grid.GetTerrain(new Cell(x, y)));
            }
        }
    }

    [Fact]
    public void SetTerrain_ChangesCell()
    {
        var grid = new TerrainGrid(4, 3);
        var cell = new Cell(2, 1);

        grid.SetTerrain(cell, TerrainType.Water);

        Assert.Equal(TerrainType.Water, grid.GetTerrain(cell));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(4, 0)]
    [InlineData(0, 3)]
    public void GetTerrain_OutsideMap_Throws(int x, int y)
    {
        var grid = new TerrainGrid(4, 3);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => grid.GetTerrain(new Cell(x, y)));
    }

    [Fact]
    public void SeededRandom_SameSeed_GivesSameSequence()
    {
        var first = new SeededRandom(12345);
        var second = new SeededRandom(12345);

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(first.NextUInt64(), second.NextUInt64());
            Assert.Equal(
                first.NextInt(0, 100),
                second.NextInt(0, 100));
        }
    }

    [Fact]
    public void SeededRandom_DifferentSeeds_GiveDifferentSequences()
    {
        var first = new SeededRandom(1);
        var second = new SeededRandom(2);

        var firstValues = new ulong[10];
        var secondValues = new ulong[10];

        for (var i = 0; i < firstValues.Length; i++)
        {
            firstValues[i] = first.NextUInt64();
            secondValues[i] = second.NextUInt64();
        }

        Assert.NotEqual(firstValues, secondValues);
    }

    [Fact]
    public void NextInt_ReturnsValueInRange()
    {
        var random = new SeededRandom(42);

        for (var i = 0; i < 1000; i++)
        {
            var value = random.NextInt(3, 10);

            Assert.InRange(value, 3, 9);
        }
    }

    [Fact]
    public void NextDouble_ReturnsValueInRange()
    {
        var random = new SeededRandom(42);

        for (var i = 0; i < 1000; i++)
        {
            var value = random.NextDouble();

            Assert.InRange(value, 0.0, 1.0);
        }
    }
}