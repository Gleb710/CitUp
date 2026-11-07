namespace CitUp.Sim;

/// <summary>
/// Все игровые данные, необходимые для создания и запуска партии.
/// Данные читаются из JSON и проверяются при загрузке.
/// </summary>
public sealed record GameDataCatalog(
    ResourceCatalog Resources,
    BuildingCatalog Buildings,
    RecipeCatalog Recipes,
    ProfessionCatalog Professions,
    StartupConfiguration Startup)
{
    public static GameDataCatalog LoadDefault() =>
        FromFiles(
            GameData.LoadText("resources.json"),
            GameData.LoadText("buildings.json"),
            GameData.LoadText("recipes.json"),
            GameData.LoadText("professions.json"),
            GameData.LoadText("startup.json"));

    public static GameDataCatalog FromFiles(
        string resourcesJson,
        string buildingsJson,
        string recipesJson,
        string professionsJson,
        string startupJson)
    {
        var resources = ResourceCatalog.FromJson(resourcesJson);
        var buildings = BuildingCatalog.FromJson(buildingsJson, resources);
        var recipes = RecipeCatalog.FromJson(recipesJson, buildings, resources);
        var professions = ProfessionCatalog.FromJson(professionsJson);
        var startup = StartupConfigurationLoader.FromJson(
            startupJson,
            buildings,
            resources);

        return new GameDataCatalog(
            resources,
            buildings,
            recipes,
            professions,
            startup);
    }
}