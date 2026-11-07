using System.Text.Json;

namespace CitUp.Sim;

/// <summary>Каталог ресурсов и денег, загруженный из Data/resources.json.</summary>
public sealed class ResourceCatalog
{
    private readonly Dictionary<string, ResourceDefinition> _resourcesById;

    public MoneyDefinition Money { get; }

    public IReadOnlyList<ResourceDefinition> Resources { get; }

    public ResourceCatalog(MoneyDefinition money, IReadOnlyList<ResourceDefinition> resources)
    {
        Money = money;
        Resources = resources;
        _resourcesById = resources.ToDictionary(resource => resource.Id);
    }

    public ResourceDefinition? FindResource(string resourceId) =>
        _resourcesById.GetValueOrDefault(resourceId);

    public static ResourceCatalog LoadDefault() =>
        FromJson(GameData.LoadText("resources.json"));

    public static ResourceCatalog FromJson(string json)
    {
        ResourceCatalogData? data;

        try
        {
            data = JsonSerializer.Deserialize<ResourceCatalogData>(json, JsonOptions.Shared);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Ошибка в Data/resources.json: {exception.Message}", exception);
        }

        if (data is null)
            throw new InvalidDataException("Data/resources.json пуст.");

        Validate(data);

        return new ResourceCatalog(
            new MoneyDefinition(data.Money.Id, data.Money.Name),
            data.Resources
                .Select(resource => new ResourceDefinition(resource.Id, resource.Name))
                .ToArray());
    }

    private static void Validate(ResourceCatalogData data)
    {
        if (data.Money is null)
            throw new InvalidDataException("В Data/resources.json отсутствует money.");

        if (string.IsNullOrWhiteSpace(data.Money.Id))
            throw new InvalidDataException("В Data/resources.json у money отсутствует id.");

        if (string.IsNullOrWhiteSpace(data.Money.Name))
            throw new InvalidDataException("В Data/resources.json у money отсутствует name.");

        if (data.Resources is null || data.Resources.Count == 0)
            throw new InvalidDataException("В Data/resources.json resources не должен быть пустым.");

        var duplicateId = data.Resources
            .GroupBy(resource => resource.Id)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateId is not null)
            throw new InvalidDataException(
                $"В Data/resources.json повторяется id ресурса: {duplicateId.Key}.");

        foreach (var resource in data.Resources)
        {
            if (string.IsNullOrWhiteSpace(resource.Id))
                throw new InvalidDataException(
                    "В Data/resources.json у одного из ресурсов отсутствует id.");

            if (string.IsNullOrWhiteSpace(resource.Name))
                throw new InvalidDataException(
                    $"В Data/resources.json у ресурса {resource.Id} отсутствует name.");
        }

        if (data.Resources.Any(resource => resource.Id == data.Money.Id))
            throw new InvalidDataException(
                $"В Data/resources.json id денег совпадает с id ресурса: {data.Money.Id}.");
    }

    private sealed class ResourceCatalogData
    {
        public MoneyData? Money { get; set; }

        public List<ResourceData> Resources { get; set; } = new();
    }

    private sealed class MoneyData
    {
        public string Id { get; set; } = "";

        public string Name { get; set; } = "";
    }

    private sealed class ResourceData
    {
        public string Id { get; set; } = "";

        public string Name { get; set; } = "";
    }
}