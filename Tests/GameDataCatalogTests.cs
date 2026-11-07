using CitUp.Sim;
using Xunit;

namespace Tests;

public class GameDataCatalogTests
{
    [Fact]
    public void LoadDefault_LoadsAllGameData()
    {
        var gameData = GameDataCatalog.LoadDefault();

        Assert.Equal(5, gameData.Resources.Resources.Count);
        Assert.Equal(7, gameData.Buildings.Buildings.Count);
        Assert.Equal(5, gameData.Recipes.Recipes.Count);
        Assert.Equal(2, gameData.Professions.Professions.Count);

        Assert.Equal(1000, gameData.Startup.StartingMoney);
        Assert.Equal(3, gameData.Startup.FreeBuildingIds.Count);
    }

    [Fact]
    public void LoadDefault_LinksRecipesToBuildingsAndResources()
    {
        var gameData = GameDataCatalog.LoadDefault();

        var sawmillRecipe = gameData.Recipes.FindRecipe("sawmill_planks")!;

        Assert.NotNull(gameData.Buildings.FindBuilding(sawmillRecipe.BuildingId));

        foreach (var input in sawmillRecipe.Inputs)
            Assert.NotNull(gameData.Resources.FindResource(input.ResourceId));

        foreach (var output in sawmillRecipe.Outputs)
            Assert.NotNull(gameData.Resources.FindResource(output.ResourceId));
    }

    [Fact]
    public void LoadDefault_StartupBuildingsExist()
    {
        var gameData = GameDataCatalog.LoadDefault();

        foreach (var buildingId in gameData.Startup.FreeBuildingIds)
            Assert.NotNull(gameData.Buildings.FindBuilding(buildingId));
    }

    [Fact]
    public void FromFiles_BrokenResourcesJson_ThrowsClearError()
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => GameDataCatalog.FromFiles(
                "{ not json",
                GameData.LoadText("buildings.json"),
                GameData.LoadText("recipes.json"),
                GameData.LoadText("professions.json"),
                GameData.LoadText("startup.json")));

        Assert.Contains("Data/resources.json", exception.Message);
    }

    [Fact]
    public void FromFiles_BrokenBuildingsJson_ThrowsClearError()
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => GameDataCatalog.FromFiles(
                GameData.LoadText("resources.json"),
                "{ not json",
                GameData.LoadText("recipes.json"),
                GameData.LoadText("professions.json"),
                GameData.LoadText("startup.json")));

        Assert.Contains("Data/buildings.json", exception.Message);
    }

    [Fact]
    public void FromFiles_BrokenRecipesJson_ThrowsClearError()
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => GameDataCatalog.FromFiles(
                GameData.LoadText("resources.json"),
                GameData.LoadText("buildings.json"),
                "{ not json",
                GameData.LoadText("professions.json"),
                GameData.LoadText("startup.json")));

        Assert.Contains("Data/recipes.json", exception.Message);
    }

    [Fact]
    public void FromFiles_BrokenProfessionsJson_ThrowsClearError()
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => GameDataCatalog.FromFiles(
                GameData.LoadText("resources.json"),
                GameData.LoadText("buildings.json"),
                GameData.LoadText("recipes.json"),
                "{ not json",
                GameData.LoadText("startup.json")));

        Assert.Contains("Data/professions.json", exception.Message);
    }

    [Fact]
    public void FromFiles_BrokenStartupJson_ThrowsClearError()
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => GameDataCatalog.FromFiles(
                GameData.LoadText("resources.json"),
                GameData.LoadText("buildings.json"),
                GameData.LoadText("recipes.json"),
                GameData.LoadText("professions.json"),
                "{ not json"));

        Assert.Contains("Data/startup.json", exception.Message);
    }
}