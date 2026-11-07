using CitUp.Sim;
using Xunit;

namespace Tests;

public class RecipeCatalogTests
{
    [Fact]
    public void LoadDefault_ContainsFiveRecipes()
    {
        var catalog = RecipeCatalog.LoadDefault();

        Assert.Equal(5, catalog.Recipes.Count);

        Assert.NotNull(catalog.FindRecipe("well_water"));
        Assert.NotNull(catalog.FindRecipe("fisherman_fish"));
        Assert.NotNull(catalog.FindRecipe("woodcutter_wood"));
        Assert.NotNull(catalog.FindRecipe("sawmill_planks"));
        Assert.NotNull(catalog.FindRecipe("mine_stone"));

        Assert.Null(catalog.FindRecipe("unknown_recipe"));
    }

    [Fact]
    public void SawmillRecipe_HasInputAndOutput()
    {
        var recipe = RecipeCatalog.LoadDefault().FindRecipe("sawmill_planks")!;

        Assert.Equal("sawmill", recipe.BuildingId);
        Assert.Equal(200, recipe.CycleTicks);

        var input = Assert.Single(recipe.Inputs);
        Assert.Equal("wood", input.ResourceId);
        Assert.Equal(2, input.Amount);

        var output = Assert.Single(recipe.Outputs);
        Assert.Equal("planks", output.ResourceId);
        Assert.Equal(4, output.Amount);
    }

    [Fact]
    public void FindRecipesForBuilding_ReturnsOnlyLinkedRecipes()
    {
        var catalog = RecipeCatalog.LoadDefault();

        var sawmillRecipes = catalog.FindRecipesForBuilding("sawmill");

        var recipe = Assert.Single(sawmillRecipes);
        Assert.Equal("sawmill_planks", recipe.Id);

        Assert.Empty(catalog.FindRecipesForBuilding("house"));
    }

    [Fact]
    public void FromJson_UnknownBuilding_Throws()
    {
        const string json = """
            {
              "recipes": [
                {
                  "id": "test_recipe",
                  "buildingId": "castle",
                  "inputs": [],
                  "outputs": [
                    { "resourceId": "wood", "amount": 1 }
                  ],
                  "cycleTicks": 100
                }
              ]
            }
            """;

        var exception = Assert.Throws<InvalidDataException>(
            () => RecipeCatalog.FromJson(json));

        Assert.Contains("castle", exception.Message);
    }

    [Fact]
    public void FromJson_UnknownResource_Throws()
    {
        const string json = """
            {
              "recipes": [
                {
                  "id": "test_recipe",
                  "buildingId": "well",
                  "inputs": [],
                  "outputs": [
                    { "resourceId": "gold", "amount": 1 }
                  ],
                  "cycleTicks": 100
                }
              ]
            }
            """;

        var exception = Assert.Throws<InvalidDataException>(
            () => RecipeCatalog.FromJson(json));

        Assert.Contains("gold", exception.Message);
    }

    [Fact]
    public void FromJson_ZeroCycleTicks_Throws()
    {
        const string json = """
            {
              "recipes": [
                {
                  "id": "test_recipe",
                  "buildingId": "well",
                  "inputs": [],
                  "outputs": [
                    { "resourceId": "water", "amount": 1 }
                  ],
                  "cycleTicks": 0
                }
              ]
            }
            """;

        Assert.Throws<InvalidDataException>(
            () => RecipeCatalog.FromJson(json));
    }

    [Fact]
    public void FromJson_EmptyOutputs_Throws()
    {
        const string json = """
            {
              "recipes": [
                {
                  "id": "test_recipe",
                  "buildingId": "well",
                  "inputs": [],
                  "outputs": [],
                  "cycleTicks": 100
                }
              ]
            }
            """;

        Assert.Throws<InvalidDataException>(
            () => RecipeCatalog.FromJson(json));
    }

    [Fact]
    public void FromJson_DuplicateRecipeId_Throws()
    {
        const string json = """
            {
              "recipes": [
                {
                  "id": "same_recipe",
                  "buildingId": "well",
                  "inputs": [],
                  "outputs": [
                    { "resourceId": "water", "amount": 1 }
                  ],
                  "cycleTicks": 100
                },
                {
                  "id": "same_recipe",
                  "buildingId": "well",
                  "inputs": [],
                  "outputs": [
                    { "resourceId": "water", "amount": 1 }
                  ],
                  "cycleTicks": 100
                }
              ]
            }
            """;

        Assert.Throws<InvalidDataException>(
            () => RecipeCatalog.FromJson(json));
    }

    [Fact]
    public void FromJson_BrokenJson_ThrowsClearError()
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => RecipeCatalog.FromJson("{ not json"));

        Assert.Contains("Data/recipes.json", exception.Message);
    }
}