using CitUp.Sim;
using Xunit;

namespace Tests;

public class MapGenerationSettingsTests
{
    [Fact]
    public void LoadDefault_HasExpectedSettings()
    {
        var settings = MapGenerationSettingsLoader.LoadDefault();

        Assert.Equal(80, settings.Width);
        Assert.Equal(100, settings.Height);

        Assert.Equal(3, settings.LakeCount);
        Assert.Equal(8, settings.MinLakeSize);
        Assert.Equal(20, settings.MaxLakeSize);

        Assert.Equal(4, settings.CliffClusterCount);
        Assert.Equal(6, settings.MinCliffSize);
        Assert.Equal(16, settings.MaxCliffSize);

        Assert.Equal(3, settings.ResourceVeinCount);
        Assert.Equal(12, settings.ForestClusterCount);
        Assert.Equal(0.35, settings.SlopeChance);
    }

    [Fact]
    public void ToOptions_UsesSeed()
    {
        var settings = MapGenerationSettingsLoader.LoadDefault();

        var options = settings.ToOptions(12345);

        Assert.Equal(12345UL, options.Seed);
        Assert.Equal(settings.Width, options.Width);
        Assert.Equal(settings.Height, options.Height);
        Assert.Equal(settings.LakeCount, options.LakeCount);
    }

    [Fact]
    public void FromJson_ZeroMapWidth_Throws()
    {
        const string json = """
            {
              "map": {
                "width": 0,
                "height": 32
              },
              "lakeCount": 3,
              "minLakeSize": 8,
              "maxLakeSize": 20,
              "cliffClusterCount": 4,
              "minCliffSize": 6,
              "maxCliffSize": 16,
              "resourceVeinCount": 3,
              "forestClusterCount": 12,
              "slopeChance": 0.35
            }
            """;

        Assert.Throws<InvalidDataException>(
            () => MapGenerationSettingsLoader.FromJson(json));
    }

    [Fact]
    public void FromJson_NegativeLakeCount_Throws()
    {
        const string json = """
            {
              "map": {
                "width": 32,
                "height": 32
              },
              "lakeCount": -1,
              "minLakeSize": 8,
              "maxLakeSize": 20,
              "cliffClusterCount": 4,
              "minCliffSize": 6,
              "maxCliffSize": 16,
              "resourceVeinCount": 3,
              "forestClusterCount": 12,
              "slopeChance": 0.35
            }
            """;

        Assert.Throws<InvalidDataException>(
            () => MapGenerationSettingsLoader.FromJson(json));
    }

    [Fact]
    public void FromJson_InvalidSlopeChance_Throws()
    {
        const string json = """
            {
              "map": {
                "width": 32,
                "height": 32
              },
              "lakeCount": 3,
              "minLakeSize": 8,
              "maxLakeSize": 20,
              "cliffClusterCount": 4,
              "minCliffSize": 6,
              "maxCliffSize": 16,
              "resourceVeinCount": 3,
              "forestClusterCount": 12,
              "slopeChance": 1.5
            }
            """;

        Assert.Throws<InvalidDataException>(
            () => MapGenerationSettingsLoader.FromJson(json));
    }

    [Fact]
    public void FromJson_BrokenJson_ThrowsClearError()
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => MapGenerationSettingsLoader.FromJson("{ not json"));

        Assert.Contains("Data/map_generation.json", exception.Message);
    }
}