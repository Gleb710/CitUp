using System.Text.Json;

namespace CitUp.Sim;

/// <summary>Загружает и проверяет Data/startup.json.</summary>
public static class StartupConfigurationLoader
{
    public static StartupConfiguration LoadDefault() =>
        FromJson(
            GameData.LoadText("startup.json"),
            BuildingCatalog.LoadDefault(),
            ResourceCatalog.LoadDefault());

    public static StartupConfiguration FromJson(string json) =>
        FromJson(
            json,
            BuildingCatalog.LoadDefault(),
            ResourceCatalog.LoadDefault());

    public static StartupConfiguration FromJson(
        string json,
        BuildingCatalog buildings,
        ResourceCatalog resources)
    {
        StartupConfigurationData? data;

        try
        {
            data = JsonSerializer.Deserialize<StartupConfigurationData>(json, JsonOptions.Shared);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Ошибка в Data/startup.json: {exception.Message}", exception);
        }

        if (data is null)
            throw new InvalidDataException("Data/startup.json пуст.");

        Validate(data, buildings, resources);

        var startingResourcesData = data.StartingResources
                                    ?? throw new InvalidDataException(
                                        "В Data/startup.json startingResources отсутствует.");

        var freeBuildingIds = data.FreeBuildingIds
                              ?? throw new InvalidDataException(
                                  "В Data/startup.json freeBuildingIds отсутствует.");

        var startingResources = startingResourcesData
            .Select(resource => new ResourceAmount(resource.ResourceId, resource.Amount))
            .ToArray();

        return new StartupConfiguration(
            data.StartingMoney,
            startingResources,
            freeBuildingIds);
    }

    private static void Validate(
        StartupConfigurationData data,
        BuildingCatalog buildings,
        ResourceCatalog resources)
    {
        if (data.StartingMoney < 0)
            throw new InvalidDataException(
                "В Data/startup.json startingMoney не может быть отрицательным.");

        ValidateResources(data, resources);
        ValidateFreeBuildings(data, buildings);
    }

    private static void ValidateResources(
        StartupConfigurationData data,
        ResourceCatalog resources)
    {
        if (data.StartingResources is null)
            throw new InvalidDataException(
                "В Data/startup.json startingResources отсутствует.");

        foreach (var resource in data.StartingResources)
        {
            if (string.IsNullOrWhiteSpace(resource.ResourceId))
                throw new InvalidDataException(
                    "В Data/startup.json у одного из стартовых ресурсов отсутствует resourceId.");

            if (resources.FindResource(resource.ResourceId) is null)
                throw new InvalidDataException(
                    $"В Data/startup.json стартовый ресурс не существует: {resource.ResourceId}.");

            if (resource.Amount < 0)
                throw new InvalidDataException(
                    $"В Data/startup.json количество ресурса {resource.ResourceId} " +
                    "не может быть отрицательным.");
        }
    }

    private static void ValidateFreeBuildings(
        StartupConfigurationData data,
        BuildingCatalog buildings)
    {
        if (data.FreeBuildingIds is null)
            throw new InvalidDataException(
                "В Data/startup.json freeBuildingIds отсутствует.");

        var duplicateBuilding = data.FreeBuildingIds
            .GroupBy(buildingId => buildingId)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateBuilding is not null)
            throw new InvalidDataException(
                $"В Data/startup.json повторяется бесплатное здание: {duplicateBuilding.Key}.");

        foreach (var buildingId in data.FreeBuildingIds)
        {
            if (buildings.FindBuilding(buildingId) is null)
                throw new InvalidDataException(
                    $"В Data/startup.json бесплатное здание не существует: {buildingId}.");
        }
    }

    private sealed class StartupConfigurationData
    {
        public int StartingMoney { get; set; }

        public List<ResourceAmountData>? StartingResources { get; set; }

        public List<string>? FreeBuildingIds { get; set; }
    }

    private sealed class ResourceAmountData
    {
        public string ResourceId { get; set; } = "";

        public int Amount { get; set; }
    }
}