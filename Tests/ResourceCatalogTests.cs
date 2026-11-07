using CitUp.Sim;
using Xunit;

namespace Tests;

public class ResourceCatalogTests
{
    [Fact]
    public void LoadDefault_ContainsFiveResources_AndMoney()
    {
        var catalog = ResourceCatalog.LoadDefault();

        Assert.Equal(5, catalog.Resources.Count);
        Assert.Equal("Деньги", catalog.Money.Name);

        Assert.NotNull(catalog.FindResource("wood"));
        Assert.NotNull(catalog.FindResource("planks"));
        Assert.NotNull(catalog.FindResource("stone"));
        Assert.NotNull(catalog.FindResource("water"));
        Assert.NotNull(catalog.FindResource("fish"));

        Assert.Null(catalog.FindResource("gold"));
    }

    [Fact]
    public void FindResource_ReturnsCorrectName()
    {
        var catalog = ResourceCatalog.LoadDefault();

        var wood = catalog.FindResource("wood");

        Assert.NotNull(wood);
        Assert.Equal("Древесина", wood.Name);
    }

    [Fact]
    public void FromJson_DuplicateResourceId_Throws()
    {
        const string json = """
            {
              "money": { "id": "money", "name": "Деньги" },
              "resources": [
                { "id": "wood", "name": "Древесина" },
                { "id": "wood", "name": "Древесина" }
              ]
            }
            """;

        var exception = Assert.Throws<InvalidDataException>(
            () => ResourceCatalog.FromJson(json));

        Assert.Contains("wood", exception.Message);
    }

    [Fact]
    public void FromJson_EmptyResources_Throws()
    {
        const string json = """
            {
              "money": { "id": "money", "name": "Деньги" },
              "resources": []
            }
            """;

        Assert.Throws<InvalidDataException>(
            () => ResourceCatalog.FromJson(json));
    }

    [Fact]
    public void FromJson_MoneyIdSameAsResourceId_Throws()
    {
        const string json = """
            {
              "money": { "id": "wood", "name": "Деньги" },
              "resources": [
                { "id": "wood", "name": "Древесина" }
              ]
            }
            """;

        Assert.Throws<InvalidDataException>(
            () => ResourceCatalog.FromJson(json));
    }

    [Fact]
    public void FromJson_BrokenJson_ThrowsClearError()
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => ResourceCatalog.FromJson("{ not json"));

        Assert.Contains("Data/resources.json", exception.Message);
    }
}