using CitUp.Sim;
using Xunit;

namespace Tests;

public class ProfessionCatalogTests
{
    [Fact]
    public void LoadDefault_ContainsBuilderAndPorter()
    {
        var catalog = ProfessionCatalog.LoadDefault();

        Assert.Equal(2, catalog.Professions.Count);

        var builder = catalog.FindProfession("builder");
        var porter = catalog.FindProfession("porter");

        Assert.NotNull(builder);
        Assert.NotNull(porter);

        Assert.Equal("Строитель", builder.Name);
        Assert.Equal("Носильщик", porter.Name);

        Assert.Equal(600, builder.WorkTicks);
        Assert.Equal(300, builder.RestTicks);

        Assert.Equal(600, porter.WorkTicks);
        Assert.Equal(300, porter.RestTicks);

        Assert.Null(catalog.FindProfession("miner"));
    }

    [Fact]
    public void FromJson_DuplicateProfessionId_Throws()
    {
        const string json = """
            {
              "professions": [
                {
                  "id": "builder",
                  "name": "Строитель",
                  "workTicks": 600,
                  "restTicks": 300
                },
                {
                  "id": "builder",
                  "name": "Строитель",
                  "workTicks": 600,
                  "restTicks": 300
                }
              ]
            }
            """;

        var exception = Assert.Throws<InvalidDataException>(
            () => ProfessionCatalog.FromJson(json));

        Assert.Contains("builder", exception.Message);
    }

    [Fact]
    public void FromJson_ZeroWorkTicks_Throws()
    {
        const string json = """
            {
              "professions": [
                {
                  "id": "builder",
                  "name": "Строитель",
                  "workTicks": 0,
                  "restTicks": 300
                }
              ]
            }
            """;

        Assert.Throws<InvalidDataException>(
            () => ProfessionCatalog.FromJson(json));
    }

    [Fact]
    public void FromJson_NegativeRestTicks_Throws()
    {
        const string json = """
            {
              "professions": [
                {
                  "id": "builder",
                  "name": "Строитель",
                  "workTicks": 600,
                  "restTicks": -1
                }
              ]
            }
            """;

        Assert.Throws<InvalidDataException>(
            () => ProfessionCatalog.FromJson(json));
    }

    [Fact]
    public void FromJson_BrokenJson_ThrowsClearError()
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => ProfessionCatalog.FromJson("{ not json"));

        Assert.Contains("Data/professions.json", exception.Message);
    }
}