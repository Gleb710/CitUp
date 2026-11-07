using System.Text.Json;

namespace CitUp.Sim;

/// <summary>Каталог рецептов, загруженный из Data/recipes.json.</summary>
public sealed class RecipeCatalog
{
    private readonly Dictionary<string, RecipeDefinition> _recipesById;

    public IReadOnlyList<RecipeDefinition> Recipes { get; }

    public RecipeCatalog(IReadOnlyList<RecipeDefinition> recipes)
    {
        Recipes = recipes;
        _recipesById = recipes.ToDictionary(recipe => recipe.Id);
    }

    public RecipeDefinition? FindRecipe(string recipeId) =>
        _recipesById.GetValueOrDefault(recipeId);

    public IReadOnlyList<RecipeDefinition> FindRecipesForBuilding(string buildingId) =>
        Recipes
            .Where(recipe => recipe.BuildingId == buildingId)
            .ToArray();

    public static RecipeCatalog LoadDefault() =>
        FromJson(
            GameData.LoadText("recipes.json"),
            BuildingCatalog.LoadDefault(),
            ResourceCatalog.LoadDefault());

    public static RecipeCatalog FromJson(string json) =>
        FromJson(
            json,
            BuildingCatalog.LoadDefault(),
            ResourceCatalog.LoadDefault());

    public static RecipeCatalog FromJson(
        string json,
        BuildingCatalog buildings,
        ResourceCatalog resources)
    {
        RecipeCatalogData? data;

        try
        {
            data = JsonSerializer.Deserialize<RecipeCatalogData>(json, JsonOptions.Shared);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Ошибка в Data/recipes.json: {exception.Message}", exception);
        }

        if (data is null)
            throw new InvalidDataException("Data/recipes.json пуст.");

        Validate(data, buildings, resources);

        var recipes = data.Recipes
            .Select(recipe => new RecipeDefinition(
                recipe.Id,
                recipe.BuildingId,
                CreateAmounts(recipe.Inputs),
                CreateAmounts(recipe.Outputs),
                recipe.CycleTicks))
            .ToArray();

        return new RecipeCatalog(recipes);
    }

    private static ResourceAmount[] CreateAmounts(List<ResourceAmountData>? amounts) =>
        amounts?
            .Select(amount => new ResourceAmount(amount.ResourceId, amount.Amount))
            .ToArray()
        ?? Array.Empty<ResourceAmount>();

    private static void Validate(
        RecipeCatalogData data,
        BuildingCatalog buildings,
        ResourceCatalog resources)
    {
        if (data.Recipes is null || data.Recipes.Count == 0)
            throw new InvalidDataException(
                "В Data/recipes.json recipes не должен быть пустым.");

        var duplicateId = data.Recipes
            .GroupBy(recipe => recipe.Id)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateId is not null)
            throw new InvalidDataException(
                $"В Data/recipes.json повторяется id рецепта: {duplicateId.Key}.");

        foreach (var recipe in data.Recipes)
        {
            if (string.IsNullOrWhiteSpace(recipe.Id))
                throw new InvalidDataException(
                    "В Data/recipes.json у одного из рецептов отсутствует id.");

            if (string.IsNullOrWhiteSpace(recipe.BuildingId))
                throw new InvalidDataException(
                    $"В Data/recipes.json у рецепта {recipe.Id} отсутствует buildingId.");

            if (buildings.FindBuilding(recipe.BuildingId) is null)
                throw new InvalidDataException(
                    $"В Data/recipes.json рецепт {recipe.Id} ссылается " +
                    $"на неизвестное здание: {recipe.BuildingId}.");

            if (recipe.CycleTicks <= 0)
                throw new InvalidDataException(
                    $"В Data/recipes.json у рецепта {recipe.Id} cycleTicks должен быть больше 0.");

            ValidateAmounts(
                recipe.Id,
                "inputs",
                recipe.Inputs,
                resources,
                allowEmpty: true);

            ValidateAmounts(
                recipe.Id,
                "outputs",
                recipe.Outputs,
                resources,
                allowEmpty: false);
        }
    }

    private static void ValidateAmounts(
        string recipeId,
        string fieldName,
        List<ResourceAmountData>? amounts,
        ResourceCatalog resources,
        bool allowEmpty)
    {
        if (amounts is null || amounts.Count == 0)
        {
            if (allowEmpty)
                return;

            throw new InvalidDataException(
                $"В Data/recipes.json у рецепта {recipeId} {fieldName} не должен быть пустым.");
        }

        foreach (var amount in amounts)
        {
            if (string.IsNullOrWhiteSpace(amount.ResourceId))
                throw new InvalidDataException(
                    $"В Data/recipes.json у рецепта {recipeId} в {fieldName} " +
                    "у одного из ресурсов отсутствует resourceId.");

            if (resources.FindResource(amount.ResourceId) is null)
                throw new InvalidDataException(
                    $"В Data/recipes.json рецепт {recipeId} в {fieldName} ссылается " +
                    $"на неизвестный ресурс: {amount.ResourceId}.");

            if (amount.Amount <= 0)
                throw new InvalidDataException(
                    $"В Data/recipes.json у рецепта {recipeId} в {fieldName} " +
                    $"недопустимое количество ресурса {amount.ResourceId}: {amount.Amount}.");
        }
    }

    private sealed class RecipeCatalogData
    {
        public List<RecipeData> Recipes { get; set; } = new();
    }

    private sealed class RecipeData
    {
        public string Id { get; set; } = "";

        public string BuildingId { get; set; } = "";

        public List<ResourceAmountData>? Inputs { get; set; }

        public List<ResourceAmountData>? Outputs { get; set; }

        public int CycleTicks { get; set; }
    }

    private sealed class ResourceAmountData
    {
        public string ResourceId { get; set; } = "";

        public int Amount { get; set; }
    }
}