using CitUp.Sim;
using Xunit;

namespace Tests;

public sealed class BuildingRotationTests
{
    private static BuildingMask Sample() => new(new[] { "FP.", "..C" });

    [Fact]
    public void Rotate_90_TurnsClockwise() =>
        Assert.Equal(new[] { ".F", ".P", "C." }, Sample().Rotate(90).Rows);

    [Fact]
    public void Rotate_180_TurnsHalfway() =>
        Assert.Equal(new[] { "C..", ".PF" }, Sample().Rotate(180).Rows);

    [Fact]
    public void Rotate_270_TurnsCounterClockwise() =>
        Assert.Equal(new[] { ".C", "P.", "F." }, Sample().Rotate(270).Rows);

    [Fact]
    public void Rotate_0_ReturnsSameMask()
    {
        var mask = Sample();

        Assert.Same(mask, mask.Rotate(0));
    }

    [Fact]
    public void FourQuarterTurns_ReturnOriginal()
    {
        var mask = Sample();

        var result = mask.Rotate(90).Rotate(90).Rotate(90).Rotate(90);

        Assert.Equal(mask.Rows, result.Rows);
    }

    [Fact]
    public void Rotate_InvalidDegrees_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Sample().Rotate(45));

    [Fact]
    public void Fisherman_Rotated90_KeepsMasksConsistent()
    {
        var fisherman = BuildingCatalog.LoadDefault().FindBuilding("fisherman");
        Assert.NotNull(fisherman);

        var origin = new Cell(10, 20);
        var footprint = BuildingGeometry.GetFootprintCells(fisherman!, origin, 90);
        var clearance = BuildingGeometry.GetClearanceCells(fisherman!, origin, 90);
        var access = BuildingGeometry.GetAccessCells(fisherman!, origin, 90);

        Assert.Equal(4, footprint.Count);
        Assert.Contains(new Cell(11, 21), footprint);
        Assert.Contains(new Cell(12, 22), footprint);

        Assert.Equal(12, clearance.Count);
        Assert.Equal(4, access.Count);
        Assert.All(access, cell => Assert.Equal(14, cell.X));

        Assert.Empty(footprint.Intersect(clearance));
        Assert.Empty(footprint.Intersect(access));
        Assert.Empty(clearance.Intersect(access));
    }

    [Fact]
    public void SupportsRotation_HouseOnlyAtZero()
    {
        var house = BuildingCatalog.LoadDefault().FindBuilding("house")!;

        Assert.True(house.SupportsRotation(0));
        Assert.False(house.SupportsRotation(90));
    }

    [Fact]
    public void SupportsRotation_FishermanAllFour()
    {
        var fisherman = BuildingCatalog.LoadDefault().FindBuilding("fisherman")!;

        Assert.True(fisherman.SupportsRotation(0));
        Assert.True(fisherman.SupportsRotation(90));
        Assert.True(fisherman.SupportsRotation(180));
        Assert.True(fisherman.SupportsRotation(270));
    }
}