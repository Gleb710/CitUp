using System.Text.Json;
using System.Text.RegularExpressions;

namespace CitUp.Sim;

/// <summary>Каталог зданий, загруженный из Data/buildings.json.</summary>
public sealed class BuildingCatalog
{
    private readonly Dictionary<string, BuildingDefinition> _buildingsById;

    private static readonly Regex ColorPattern = new("^#[0-9a-fA-F]{6}$");

    public IReadOnlyList<BuildingDefinition> BuildableBuildings { get; }
    
    public IReadOnlyList<BuildingDefinition> Buildings { get; }

    public BuildingCatalog(IReadOnlyList<BuildingDefinition> buildings)
    {
        Buildings = buildings;
        BuildableBuildings = buildings.Where(building => building.Buildable).ToArray();
        _buildingsById = buildings.ToDictionary(building => building.Id);
    }

    public BuildingDefinition? FindBuilding(string buildingId) =>
        _buildingsById.GetValueOrDefault(buildingId);

    public static BuildingCatalog LoadDefault() =>
        FromJson(
            GameData.LoadText("buildings.json"),
            ResourceCatalog.LoadDefault());

    public static BuildingCatalog FromJson(string json) =>
        FromJson(json, ResourceCatalog.LoadDefault());

    public static BuildingCatalog FromJson(string json, ResourceCatalog resources)
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

        Validate(data, resources);

        var buildings = data.Buildings
            .Select(building => new BuildingDefinition(
                building.Id,
                building.Name,
                new BuildingMask(building.Footprint),
                new BuildingMask(building.Clearance),
                building.Access is null ? null : new BuildingMask(building.Access),
                building.Rotations,
                CreateCost(building),
                building.ConstructionTimeTicks,
                building.Color,
                building.Buildable))
            .ToArray();

        foreach (var building in buildings)
            ValidateGeometry(building);
        
        return new BuildingCatalog(buildings);
    }

    private static BuildingCost CreateCost(BuildingData building)
    {
        var cost = building.Cost
                   ?? throw new InvalidDataException(
                       $"В Data/buildings.json у здания {building.Id} отсутствует cost.");

        var resources = cost.Resources
                        ?? throw new InvalidDataException(
                            $"В Data/buildings.json у здания {building.Id} cost.resources отсутствует.");

        var amounts = resources
            .Select(resource => new ResourceAmount(resource.Key, resource.Value))
            .ToArray();

        return new BuildingCost(cost.Money, amounts);
    }

    private static void Validate(BuildingCatalogData data, ResourceCatalog resources)
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

            if (!ColorPattern.IsMatch(building.Color ?? ""))
                throw new InvalidDataException(
                    $"В Data/buildings.json у здания {building.Id} цвет должен быть в формате #RRGGBB.");
            
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

            ValidateRotations(building.Id, building.Rotations);

            ValidateCost(building, resources);
        }
    }

    private static void ValidateGeometry(BuildingDefinition building)
    {
        var origin = new Cell(0, 0);

        IReadOnlyList<Cell> footprint;
        IReadOnlyList<Cell> clearance;

        try
        {
            footprint = BuildingGeometry.GetFootprintCells(building, origin);
            clearance = BuildingGeometry.GetClearanceCells(building, origin);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidDataException(
                $"В Data/buildings.json у здания {building.Id} маски нельзя совместить: {exception.Message}",
                exception);
        }

        var overlap = footprint.Intersect(clearance).Cast<Cell?>().FirstOrDefault();

        if (overlap is not null)
            throw new InvalidDataException(
                $"В Data/buildings.json у здания {building.Id} footprint и clearance " +
                $"пересекаются в клетке {overlap}.");
    }
    
    private static void ValidateCost(
        BuildingData building,
        ResourceCatalog resources)
    {
        var cost = building.Cost
                   ?? throw new InvalidDataException(
                       $"В Data/buildings.json у здания {building.Id} отсутствует cost.");

        if (cost.Money < 0)
            throw new InvalidDataException(
                $"В Data/buildings.json у здания {building.Id} стоимость денег не может быть отрицательной.");

        if (building.ConstructionTimeTicks < 0)
            throw new InvalidDataException(
                $"В Data/buildings.json у здания {building.Id} constructionTimeTicks не может быть отрицательным.");

        var costResources = cost.Resources
                            ?? throw new InvalidDataException(
                                $"В Data/buildings.json у здания {building.Id} cost.resources отсутствует.");

        foreach (var resource in costResources)
        {
            var resourceId = resource.Key;
            var amount = resource.Value;

            if (resources.FindResource(resourceId) is null)
                throw new InvalidDataException(
                    $"В Data/buildings.json здание {building.Id} ссылается " +
                    $"на неизвестный ресурс: {resourceId}.");

            if (amount <= 0)
                throw new InvalidDataException(
                    $"В Data/buildings.json здание {building.Id} имеет " +
                    $"недопустимое количество ресурса {resourceId}: {amount}.");
        }
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

        public BuildingCostData? Cost { get; set; }

        public int ConstructionTimeTicks { get; set; }

        public List<string> Footprint { get; set; } = new();

        public List<string> Clearance { get; set; } = new();

        public List<string>? Access { get; set; }

        public List<int> Rotations { get; set; } = new();
        
        public string Color { get; set; } = "#ffffff";

        public bool Buildable { get; set; } = true;
    }

    private sealed class BuildingCostData
    {
        public int Money { get; set; }

        public Dictionary<string, int> Resources { get; set; } = new();
    }
}