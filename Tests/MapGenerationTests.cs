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
    
    private static MapGenerationOptions CreateOptions(
    ulong seed,
    int width = 20,
    int height = 15) =>
    new(
        width,
        height,
        seed,
        LakeCount: 3,
        MinLakeSize: 8,
        MaxLakeSize: 20);

    [Fact]
    public void Generate_ReturnsGridWithConfiguredSize()
    {
        var options = CreateOptions(42);

        var grid = MapGenerator.Generate(options);

        Assert.Equal(options.Width, grid.Width);
        Assert.Equal(options.Height, grid.Height);
    }

    [Fact]
    public void Generate_SameSeed_GivesSameMap()
    {
        var options = CreateOptions(12345);

        var first = MapGenerator.Generate(options);
        var second = MapGenerator.Generate(options);

        for (var y = 0; y < options.Height; y++)
        {
            for (var x = 0; x < options.Width; x++)
            {
                var cell = new Cell(x, y);

                Assert.Equal(
                    first.GetTerrain(cell),
                    second.GetTerrain(cell));
            }
        }
    }

    [Fact]
    public void Generate_DifferentSeed_CanGiveDifferentMap()
    {
        var first = MapGenerator.Generate(CreateOptions(1));
        var second = MapGenerator.Generate(CreateOptions(2));

        var mapsAreDifferent = false;

        for (var y = 0; y < first.Height && !mapsAreDifferent; y++)
        {
            for (var x = 0; x < first.Width; x++)
            {
                var cell = new Cell(x, y);

                if (first.GetTerrain(cell) != second.GetTerrain(cell))
                {
                    mapsAreDifferent = true;
                    break;
                }
            }
        }

        Assert.True(mapsAreDifferent);
    }

    [Fact]
    public void Generate_ContainsWater()
    {
        var grid = MapGenerator.Generate(CreateOptions(777));

        var hasWater = false;

        for (var y = 0; y < grid.Height && !hasWater; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                if (grid.GetTerrain(new Cell(x, y)) == TerrainType.Water)
                {
                    hasWater = true;
                    break;
                }
            }
        }

        Assert.True(hasWater);
    }

    [Fact]
    public void Generate_AllCellsHaveValidTerrain()
    {
        var grid = MapGenerator.Generate(CreateOptions(888));

        var validTerrainTypes = new[]
        {
            TerrainType.Plain,
            TerrainType.Forest,
            TerrainType.Slope,
            TerrainType.Cliff,
            TerrainType.ResourceCliff,
            TerrainType.Water
        };

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                Assert.Contains(
                    grid.GetTerrain(new Cell(x, y)),
                    validTerrainTypes);
            }
        }
    }

    [Fact]
    public void Generate_ZeroWidth_Throws()
    {
        var options = new MapGenerationOptions(
            0,
            10,
            1,
            1,
            1,
            2);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => MapGenerator.Generate(options));
    }

    [Fact]
    public void Generate_NegativeLakeCount_Throws()
    {
        var options = new MapGenerationOptions(
            10,
            10,
            1,
            -1,
            1,
            2);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => MapGenerator.Generate(options));
    }
    
    [Fact]
    public void Generate_ContainsCliffs()
    {
        var grid = MapGenerator.Generate(CreateOptions(101));

        var hasCliff = false;

        for (var y = 0; y < grid.Height && !hasCliff; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                if (grid.GetTerrain(new Cell(x, y)) == TerrainType.Cliff)
                {
                    hasCliff = true;
                    break;
                }
            }
        }

        Assert.True(hasCliff);
    }

    [Fact]
    public void Generate_ResourceCliffsComeInPairs()
    {
        var grid = MapGenerator.Generate(CreateOptions(202));

        var resourceCellCount = 0;

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                if (grid.GetTerrain(new Cell(x, y)) == TerrainType.ResourceCliff)
                    resourceCellCount++;
            }
        }

        Assert.True(resourceCellCount >= 2);
        Assert.Equal(0, resourceCellCount % 2);
    }

    [Fact]
    public void Generate_ResourceCliffsHaveOrthogonalNeighbor()
    {
        var grid = MapGenerator.Generate(CreateOptions(303));

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var cell = new Cell(x, y);

                if (grid.GetTerrain(cell) != TerrainType.ResourceCliff)
                    continue;

                var hasResourceNeighbor =
                    HasNeighbor(grid, cell, TerrainType.ResourceCliff);

                Assert.True(hasResourceNeighbor);
            }
        }
    }

    [Fact]
    public void Generate_SlopesAreAdjacentToCliffs()
    {
        var grid = MapGenerator.Generate(CreateOptions(404));

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var cell = new Cell(x, y);

                if (grid.GetTerrain(cell) != TerrainType.Slope)
                    continue;

                var hasCliffNeighbor =
                    HasNeighbor(grid, cell, TerrainType.Cliff) ||
                    HasNeighbor(grid, cell, TerrainType.ResourceCliff);

                Assert.True(hasCliffNeighbor);
            }
        }
    }

    [Fact]
    public void Generate_NegativeSlopeChance_Throws()
    {
        var options = new MapGenerationOptions(
            10,
            10,
            1,
            1,
            1,
            2,
            SlopeChance: -0.1);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => MapGenerator.Generate(options));
    }

    private static bool HasNeighbor(
        TerrainGrid grid,
        Cell cell,
        TerrainType terrainType)
    {
        var directions = new[]
        {
            new Cell(-1, 0),
            new Cell(1, 0),
            new Cell(0, -1),
            new Cell(0, 1)
        };

        return directions.Any(direction =>
        {
            var neighbor = new Cell(
                cell.X + direction.X,
                cell.Y + direction.Y);

            return grid.IsInside(neighbor) &&
                   grid.GetTerrain(neighbor) == terrainType;
        });
    }
    
    [Fact]
    public void Generate_ContainsForest()
    {
        var grid = MapGenerator.Generate(CreateOptions(505));

        var hasForest = false;

        for (var y = 0; y < grid.Height && !hasForest; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                if (grid.GetTerrain(new Cell(x, y)) == TerrainType.Forest)
                {
                    hasForest = true;
                    break;
                }
            }
        }

        Assert.True(hasForest);
    }

    [Fact]
    public void Generate_ForestDoesNotReplaceBlockedTerrain()
    {
        var grid = MapGenerator.Generate(CreateOptions(606));

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var terrain = grid.GetTerrain(new Cell(x, y));

                if (terrain != TerrainType.Forest)
                    continue;

                Assert.NotEqual(TerrainType.Water, terrain);
                Assert.NotEqual(TerrainType.Cliff, terrain);
                Assert.NotEqual(TerrainType.ResourceCliff, terrain);
                Assert.NotEqual(TerrainType.Slope, terrain);
            }
        }
    }

    [Fact]
    public void Generate_NegativeForestClusterCount_Throws()
    {
        var options = new MapGenerationOptions(
            10,
            10,
            1,
            1,
            1,
            2,
            ForestClusterCount: -1);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => MapGenerator.Generate(options));
    }
}