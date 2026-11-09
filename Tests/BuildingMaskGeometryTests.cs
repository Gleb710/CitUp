using CitUp.Sim;
using Xunit;

namespace Tests;

public sealed class BuildingMaskGeometryTests
{
    [Fact]
    public void GetWorldCells_HouseFootprint_UsesOrigin()
    {
        var house = GetBuilding("house");

        var actual = house.Footprint.GetWorldCells(
            new Cell(10, 20),
            BuildingMaskCell.Footprint);

        var expected = new[]
        {
            new Cell(10, 20),
            new Cell(11, 20),
            new Cell(10, 21),
            new Cell(11, 21)
        };

        Assert.Equal<Cell>(expected, actual);
    }

    [Fact]
    public void GetWorldCells_HouseClearance_FormsOuterRing()
    {
        var house = GetBuilding("house");
        var footprintOrigin = new Cell(10, 20);

        var footprint = house.Footprint.GetWorldCells(
            footprintOrigin,
            BuildingMaskCell.Footprint);

        var clearance = house.Clearance.GetWorldCells(
            new Cell(9, 19),
            BuildingMaskCell.Clearance);

        Assert.Equal(12, clearance.Count);

        Assert.Contains(new Cell(9, 19), clearance);
        Assert.Contains(new Cell(12, 19), clearance);
        Assert.Contains(new Cell(9, 22), clearance);
        Assert.Contains(new Cell(12, 22), clearance);

        foreach (var cell in footprint)
            Assert.DoesNotContain(cell, clearance);
    }

    [Fact]
    public void GetWorldCells_Townhall_SeparatesBuildingAndPassableArea()
    {
        var townhall = GetBuilding("townhall");
        var origin = new Cell(10, 20);

        var blocked = townhall.Footprint.GetWorldCells(
            origin,
            BuildingMaskCell.Footprint);

        var passable = townhall.Footprint.GetWorldCells(
            origin,
            BuildingMaskCell.PassableFootprint);

        Assert.Equal(25, blocked.Count);
        Assert.Equal(10, passable.Count);

        Assert.Contains(new Cell(10, 20), blocked);
        Assert.Contains(new Cell(14, 24), blocked);

        Assert.Contains(new Cell(10, 25), passable);
        Assert.Contains(new Cell(14, 26), passable);

        foreach (var cell in passable)
            Assert.DoesNotContain(cell, blocked);
    }

    [Fact]
    public void GetWorldCells_SparseMask_DoesNotFillHoles()
    {
        var mask = new BuildingMask(new[]
        {
            "F.F",
            "...",
            ".F."
        });

        var actual = mask.GetWorldCells(
            new Cell(5, 7),
            BuildingMaskCell.Footprint);

        var expected = new[]
        {
            new Cell(5, 7),
            new Cell(7, 7),
            new Cell(6, 9)
        };

        Assert.Equal<Cell>(expected, actual);
    }

    [Fact]
    public void GetWorldCells_FishermanAccess_UsesAccessCells()
    {
        var fisherman = GetBuilding("fisherman");

        Assert.NotNull(fisherman.Access);

        var actual = fisherman.Access!.GetWorldCells(
            new Cell(10, 20),
            BuildingMaskCell.Access);

        var expected = new[]
        {
            new Cell(10, 20),
            new Cell(11, 20),
            new Cell(12, 20),
            new Cell(13, 20)
        };

        Assert.Equal<Cell>(expected, actual);
    }

    [Fact]
    public void GetWorldCells_MissingCellType_ReturnsEmpty()
    {
        var house = GetBuilding("house");

        var actual = house.Footprint.GetWorldCells(
            new Cell(10, 20),
            BuildingMaskCell.PassableFootprint);

        Assert.Empty(actual);
    }

    [Fact]
    public void GetWorldCells_NegativeOrigin_PreservesCoordinates()
    {
        var mask = new BuildingMask(new[] { "FF" });

        var actual = mask.GetWorldCells(
            new Cell(-1, -2),
            BuildingMaskCell.Footprint);

        var expected = new[]
        {
            new Cell(-1, -2),
            new Cell(0, -2)
        };

        Assert.Equal<Cell>(expected, actual);
    }

    [Fact]
    public void GetWorldCells_None_Throws()
    {
        var mask = new BuildingMask(new[] { "F" });

        Assert.Throws<ArgumentOutOfRangeException>(
            () => mask.GetWorldCells(
                new Cell(0, 0),
                BuildingMaskCell.None));
    }

    private static BuildingDefinition GetBuilding(string id)
    {
        var building = BuildingCatalog.LoadDefault().FindBuilding(id);

        Assert.NotNull(building);
        return building!;
    }
}