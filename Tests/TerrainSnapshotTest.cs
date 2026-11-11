using CitUp.Sim;
using Xunit;

namespace Tests;

public class TerrainSnapshotTests
{
    internal static void AssertSameTerrain(TerrainGrid expected, TerrainGrid actual)
    {
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);

        for (var y = 0; y < expected.Height; y++)
        {
            for (var x = 0; x < expected.Width; x++)
            {
                var cell = new Cell(x, y);

                Assert.Equal(expected.GetTerrain(cell), actual.GetTerrain(cell));
                Assert.Equal(expected.GetElevation(cell), actual.GetElevation(cell));
            }
        }
    }

    [Fact]
    public void Snapshot_ReproducesGeneratedTerrain()
    {
        var host = PartyFactory.CreateNewParty(GameDataCatalog.LoadDefault(), 12345);

        var client = World.FromSnapshot(host.Config, host.ToSnapshot());

        AssertSameTerrain(host.Terrain, client.Terrain);
    }

    [Fact]
    public void GeneratedTerrain_IsNotFlat()
    {
        var host = PartyFactory.CreateNewParty(GameDataCatalog.LoadDefault(), 12345);
        var cells = Enumerable.Range(0, host.Height)
            .SelectMany(y => Enumerable.Range(0, host.Width).Select(x => new Cell(x, y)));

        Assert.Contains(cells, cell => host.Terrain.GetTerrain(cell) != default);
    }

    [Fact]
    public void Snapshot_WrongArrayLength_ThrowsInvalidData()
    {
        var config = WorldConfig.LoadDefault();
        var bad = new WorldSnapshot(
            0, 1, Array.Empty<Building>(), Array.Empty<ResidentSnapshot>(),
            new TerrainSnapshot(2, 2, new int[3], new int[4]));

        Assert.Throws<InvalidDataException>(() => World.FromSnapshot(config, bad));
    }

    [Fact]
    public void Snapshot_TerrainSizeDiffersFromConfig_ThrowsInvalidData()
    {
        var config = WorldConfig.LoadDefault();
        var bad = new WorldSnapshot(
            0, 1, Array.Empty<Building>(), Array.Empty<ResidentSnapshot>(),
            new TerrainSnapshot(5, 5, new int[25], new int[25]));

        Assert.Throws<InvalidDataException>(() => World.FromSnapshot(config, bad));
    }

    [Fact]
    public void Snapshot_UnknownTerrainValue_ThrowsInvalidData()
    {
        var config = WorldConfig.LoadDefault();
        var count = config.Map.Width * config.Map.Height;
        var cells = new int[count];
        Array.Fill(cells, 999);

        var bad = new WorldSnapshot(
            0, 1, Array.Empty<Building>(), Array.Empty<ResidentSnapshot>(),
            new TerrainSnapshot(config.Map.Width, config.Map.Height, cells, new int[count]));

        Assert.Throws<InvalidDataException>(() => World.FromSnapshot(config, bad));
    }
}