using CitUp.Sim;
using Xunit;

namespace Tests;

public sealed class BuildingGeometryTests
{
    [Fact]
    public void House_ClearanceSurroundsFootprint()
    {
        var house = GetBuilding("house");
        var origin = new Cell(10, 20);

        var footprint = BuildingGeometry.GetFootprintCells(house, origin);
        var clearance = BuildingGeometry.GetClearanceCells(house, origin);

        Assert.Equal(4, footprint.Count);
        Assert.Equal(12, clearance.Count);

        Assert.Contains(new Cell(10, 20), footprint);
        Assert.Contains(new Cell(11, 21), footprint);

        Assert.Contains(new Cell(9, 19), clearance);
        Assert.Contains(new Cell(12, 22), clearance);

        Assert.Empty(footprint.Intersect(clearance));
    }

    [Fact]
    public void Townhall_HasBlockedPartAndPassableSquare()
    {
        var townhall = GetBuilding("townhall");
        var origin = new Cell(10, 20);

        var blocked = BuildingGeometry.GetBlockedCells(townhall, origin);
        var passable = BuildingGeometry.GetPassableCells(townhall, origin);
        var footprint = BuildingGeometry.GetFootprintCells(townhall, origin);
        var clearance = BuildingGeometry.GetClearanceCells(townhall, origin);

        Assert.Equal(25, blocked.Count);
        Assert.Equal(10, passable.Count);
        Assert.Equal(35, footprint.Count);
        Assert.Equal(28, clearance.Count);

        Assert.Contains(new Cell(10, 25), passable);
        Assert.Contains(new Cell(14, 26), passable);
        Assert.DoesNotContain(new Cell(10, 25), blocked);

        Assert.Contains(new Cell(9, 19), clearance);
        Assert.Contains(new Cell(15, 27), clearance);

        Assert.Empty(footprint.Intersect(clearance));
    }

    [Fact]
    public void Fisherman_PreservesInternalOffsets()
    {
        var fisherman = GetBuilding("fisherman");
        var origin = new Cell(10, 20);

        var footprint = BuildingGeometry.GetFootprintCells(fisherman, origin);
        var clearance = BuildingGeometry.GetClearanceCells(fisherman, origin);
        var access = BuildingGeometry.GetAccessCells(fisherman, origin);

        Assert.Equal(4, footprint.Count);
        Assert.Equal(12, clearance.Count);
        Assert.Equal(4, access.Count);

        Assert.Contains(new Cell(11, 22), footprint);
        Assert.Contains(new Cell(12, 23), footprint);
        Assert.DoesNotContain(origin, footprint);

        Assert.Contains(new Cell(10, 21), clearance);
        Assert.Contains(new Cell(13, 24), clearance);

        Assert.Contains(new Cell(10, 20), access);
        Assert.Contains(new Cell(13, 20), access);

        Assert.Empty(footprint.Intersect(clearance));
        Assert.Empty(footprint.Intersect(access));
        Assert.Empty(clearance.Intersect(access));
    }

    [Fact]
    public void Mine_FootprintIsSurroundedByClearance()
    {
        var mine = GetBuilding("mine");
        var origin = new Cell(10, 20);

        var footprint = BuildingGeometry.GetFootprintCells(mine, origin);
        var clearance = BuildingGeometry.GetClearanceCells(mine, origin);

        Assert.Equal(4, footprint.Count);
        Assert.Equal(12, clearance.Count);

        Assert.Contains(new Cell(11, 21), footprint);
        Assert.Contains(new Cell(12, 22), footprint);

        Assert.Contains(new Cell(10, 20), clearance);
        Assert.Contains(new Cell(13, 23), clearance);

        Assert.Empty(footprint.Intersect(clearance));
    }

    [Fact]
    public void House_WithoutAccess_ReturnsEmptyAccess()
    {
        var house = GetBuilding("house");

        var access = BuildingGeometry.GetAccessCells(
            house,
            new Cell(10, 20));

        Assert.Empty(access);
    }

    [Fact]
    public void Clearance_WithOddSizeDifference_Throws()
    {
        var house = GetBuilding("house") with
        {
            Clearance = new BuildingMask(new[]
            {
                "CCC",
                "C.C",
                "CCC"
            })
        };

        Assert.Throws<InvalidOperationException>(
            () => BuildingGeometry.GetClearanceCells(
                house,
                new Cell(10, 20)));
    }

    private static BuildingDefinition GetBuilding(string id)
    {
        var building = BuildingCatalog.LoadDefault().FindBuilding(id);

        Assert.NotNull(building);
        return building!;
    }
}