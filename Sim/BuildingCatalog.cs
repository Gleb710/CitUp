using System.Text.Json;

namespace CitUp.Sim;

/// <summary>Каталог зданий, загруженный из Data/buildings.json.</summary>
public sealed class BuildingCatalog
{
    private readonly Dictionary<string, BuildingDefinition> _buildingsById;

    public IReadOnlyList<BuildingDefinition> Buildings { get; }

    public BuildingCatalog(IReadOnlyList<BuildingDefinition> buildings)
    {
        Buildings = buildings;
        _buildingsById = buildings.ToDictionary(building => building.Id);
    }

    public BuildingDefinition? FindBuilding(string buildingId) =>
        _buildingsById.GetValueOrDefault(buildingId);

    public static BuildingCatalog LoadDefault() =>
        FromJson(GameData.LoadText("buildings.json"));

    public static BuildingCatalog FromJson(string json)
    {
        BuildingCatalogData? data;

        try
        {
            data = JsonSerializer.Deserialize<BuildingCatalogData>(json, JsonOptions.Shared);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Ошибка в Data/buildings.json: {exception.Message}", exception);
        }

        if (data is null)
            throw new InvalidDataException("Data/buildings.json пуст.");

        Validate(data);

        var buildings = data.Buildings
            .Select(building => new BuildingDefinition(
                building.Id,
                building.Name,
                new BuildingMask(building.Footprint),
                new BuildingMask(building.Clearance),
                building.Access is null ? null : new BuildingMask(building.Access),
                building.Rotations))
            .ToArray();

        return new BuildingCatalog(buildings);
    }

    private static void Validate(BuildingCatalogData data)
    {
        if (data.Buildings is null || data.Buildings.Count == 0)
            throw new InvalidDataException(
                "В Data/buildings.json buildings не должен быть пустым.");

        var duplicateId = data.Buildings
            .GroupBy(building => building.Id)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateId is not null)
            throw new InvalidDataException(
                $"В Data/buildings.json повторяется id здания: {duplicateId.Key}.");

        foreach (var building in data.Buildings)
        {
            if (string.IsNullOrWhiteSpace(building.Id))
                throw new InvalidDataException(
                    "В Data/buildings.json у одного из зданий отсутствует id.");

            if (string.IsNullOrWhiteSpace(building.Name))
                throw new InvalidDataException(
                    $"В Data/buildings.json у здания {building.Id} отсутствует name.");

            ValidateRotations(building.Id, building.Rotations);
            
            ValidateMask(
                building.Id,
                "footprint",
                building.Footprint,
                requireFootprint: true);

            ValidateMask(
                building.Id,
                "clearance",
                building.Clearance,
                requireFootprint: false);

            if (building.Access is not null)
                ValidateMask(
                    building.Id,
                    "access",
                    building.Access,
                    requireFootprint: false);
        }
    }

    private static void ValidateMask(
        string buildingId,
        string fieldName,
        List<string> rows,
        bool requireFootprint)
    {
        if (rows is null || rows.Count == 0)
            throw new InvalidDataException(
                $"В Data/buildings.json у здания {buildingId} маска {fieldName} пуста.");

        var width = rows[0].Length;

        if (width == 0)
            throw new InvalidDataException(
                $"В Data/buildings.json у здания {buildingId} маска {fieldName} имеет нулевую ширину.");

        for (var y = 0; y < rows.Count; y++)
        {
            var row = rows[y];

            if (row.Length != width)
                throw new InvalidDataException(
                    $"В Data/buildings.json у здания {buildingId} маска {fieldName} " +
                    $"имеет строку {y} длиной {row.Length}, а ожидается {width}.");

            foreach (var character in row)
            {
                if (character is not ('.' or 'F' or 'P' or 'C' or 'A'))
                    throw new InvalidDataException(
                        $"В Data/buildings.json у здания {buildingId} маска {fieldName} " +
                        $"содержит недопустимый символ '{character}'.");
            }
        }

        if (requireFootprint &&
            !rows.Any(row => row.Contains('F') || row.Contains('P')))
        {
            throw new InvalidDataException(
                $"В Data/buildings.json у здания {buildingId} маска footprint " +
                "не содержит ни одной клетки F или P.");
        }
    }

    private sealed class BuildingCatalogData
    {
        public List<BuildingData> Buildings { get; set; } = new();
    }

    private sealed class BuildingData
    {
        public string Id { get; set; } = "";

        public string Name { get; set; } = "";

        public List<string> Footprint { get; set; } = new();

        public List<string> Clearance { get; set; } = new();

        public List<string>? Access { get; set; }
        
        public List<int> Rotations { get; set; } = new();
    }
    
    private static void ValidateRotations(string buildingId, List<int> rotations)
    {
        var duplicateRotation = rotations
            .GroupBy(rotation => rotation)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateRotation is not null)
            throw new InvalidDataException(
                $"В Data/buildings.json у здания {buildingId} повторяется поворот: {duplicateRotation.Key}.");

        foreach (var rotation in rotations)
        {
            if (rotation is not (0 or 90 or 180 or 270))
                throw new InvalidDataException(
                    $"В Data/buildings.json у здания {buildingId} недопустимый поворот: {rotation}. " +
                    "Допустимы 0, 90, 180 и 270.");
        }
    }
}