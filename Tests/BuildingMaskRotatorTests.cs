using CitUp.Sim;
using Xunit;

namespace Tests;

public class BuildingMaskRotatorTests
{
    private static readonly string[] FishermanAccess =
    [
        "AAAA",
        "....",
        "....",
        "....",
        "...."
    ];

    [Fact]
    public void Rotate_Zero_ReturnsSameMask()
    {
        var mask = new BuildingMask(["FF", "FF"]);

        var rotated = BuildingMaskRotator.Rotate(mask, 0);

        Assert.Equal(mask, rotated);
    }

    [Fact]
    public void Fisherman_Rotate90_WaterIsOnRight()
    {
        var access = new BuildingMask(FishermanAccess);

        var rotated = BuildingMaskRotator.Rotate(access, 90);

        Assert.Equal(5, rotated.Width);
        Assert.Equal(4, rotated.Height);

        for (var y = 0; y < rotated.Height; y++)
            Assert.Equal(BuildingMaskCell.Access, rotated.GetCell(4, y));
    }

    [Fact]
    public void Fisherman_Rotate180_WaterIsOnBottom()
    {
        var access = new BuildingMask(FishermanAccess);

        var rotated = BuildingMaskRotator.Rotate(access, 180);

        Assert.Equal(4, rotated.Width);
        Assert.Equal(5, rotated.Height);

        for (var x = 0; x < rotated.Width; x++)
            Assert.Equal(BuildingMaskCell.Access, rotated.GetCell(x, 4));
    }

    [Fact]
    public void Fisherman_Rotate270_WaterIsOnLeft()
    {
        var access = new BuildingMask(FishermanAccess);

        var rotated = BuildingMaskRotator.Rotate(access, 270);

        Assert.Equal(5, rotated.Width);
        Assert.Equal(4, rotated.Height);

        for (var y = 0; y < rotated.Height; y++)
            Assert.Equal(BuildingMaskCell.Access, rotated.GetCell(0, y));
    }

    [Fact]
    public void Mine_Rotate90_ClearanceRemainsRing()
    {
        var clearance = new BuildingMask(
        [
            "CCCC",
            "C..C",
            "C..C",
            "CCCC"
        ]);

        var rotated = BuildingMaskRotator.Rotate(clearance, 90);

        Assert.Equal(4, rotated.Width);
        Assert.Equal(4, rotated.Height);

        Assert.Equal(BuildingMaskCell.Clearance, rotated.GetCell(0, 0));
        Assert.Equal(BuildingMaskCell.Clearance, rotated.GetCell(3, 0));
        Assert.Equal(BuildingMaskCell.Clearance, rotated.GetCell(0, 3));
        Assert.Equal(BuildingMaskCell.Clearance, rotated.GetCell(3, 3));

        Assert.Equal(BuildingMaskCell.None, rotated.GetCell(1, 1));
        Assert.Equal(BuildingMaskCell.None, rotated.GetCell(2, 2));
    }

    [Fact]
    public void Rotate_InvalidDegrees_Throws()
    {
        var mask = new BuildingMask(["FF", "FF"]);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => BuildingMaskRotator.Rotate(mask, 45));
    }
}