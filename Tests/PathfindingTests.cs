using CitUp.Sim;
using Xunit;

namespace Tests;

public class PathfindingTests
{
    private static World NewWorld() => new(WorldConfig.LoadDefault());

    private static void Build(World world, int id, int x, int y) =>
        world.Apply(new BuildingBuilt(id, "house", new Cell(x, y), 1));

    private static void AssertContiguous(IReadOnlyList<Cell> path)
    {
        for (int i = 1; i < path.Count; i++)
        {
            int distance = Math.Abs(path[i].X - path[i - 1].X) + Math.Abs(path[i].Y - path[i - 1].Y);
            Assert.Equal(1, distance);
        }
    }

    [Fact]
    public void Path_OnEmptyMap_IsStraightAndShortest()
    {
        var path = GridPathfinder.FindPath(NewWorld(), new Cell(0, 0), new Cell(3, 0));

        Assert.NotNull(path);
        Assert.Equal(4, path.Count);
        Assert.Equal(new Cell(0, 0), path[0]);
        Assert.Equal(new Cell(3, 0), path[^1]);
        AssertContiguous(path);
    }

    [Fact]
    public void Path_FromCellToItself_IsSingleCell()
    {
        var path = GridPathfinder.FindPath(NewWorld(), new Cell(5, 5), new Cell(5, 5));

        Assert.NotNull(path);
        Assert.Single(path);
    }

    [Fact]
    public void Path_GoesAroundBuilding()
    {
        var world = NewWorld();
        Build(world, 1, 1, 0);

        var path = GridPathfinder.FindPath(world, new Cell(0, 0), new Cell(2, 0));

        Assert.NotNull(path);
        Assert.Equal(5, path.Count);
        Assert.DoesNotContain(new Cell(1, 0), path);
        AssertContiguous(path);
    }

    [Fact]
    public void Path_WhenStartIsWalledIn_ReturnsNull()
    {
        var world = NewWorld();
        Build(world, 1, 1, 0);
        Build(world, 2, 0, 1);

        var path = GridPathfinder.FindPath(world, new Cell(0, 0), new Cell(5, 5));

        Assert.Null(path);
    }

    [Fact]
    public void Path_ToBuildingCell_ReturnsNull()
    {
        var world = NewWorld();
        Build(world, 1, 4, 4);

        Assert.Null(GridPathfinder.FindPath(world, new Cell(0, 0), new Cell(4, 4)));
    }

    [Fact]
    public void Path_ToCellOutsideMap_ReturnsNull()
    {
        Assert.Null(GridPathfinder.FindPath(NewWorld(), new Cell(0, 0), new Cell(40, 0)));
    }
}