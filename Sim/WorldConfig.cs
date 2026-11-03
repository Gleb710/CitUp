using System.Text.Json;

namespace CitUp.Sim;

public sealed class WorldConfig
{
    public MapSettings Map { get; set; } = new();
    public int TicksPerSecond { get; set; }
    public ResidentSettings Resident { get; set; } = new();
    public List<BuildingTypeSettings> BuildingTypes { get; set; } = new();

    public sealed class MapSettings
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public sealed class ResidentSettings
    {
        public int TicksPerCell { get; set; }
        public int PauseTicks { get; set; }
    }

    public sealed class BuildingTypeSettings
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Color { get; set; } = "#ffffff";
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static WorldConfig LoadDefault() => FromJson(GameData.LoadText("phase1.json"));

    public static WorldConfig FromJson(string json)
    {
        WorldConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<WorldConfig>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Ошибка в JSON конфига: {ex.Message}", ex);
        }

        if (config is null)
            throw new InvalidDataException("JSON конфига пуст.");

        config.Validate();
        return config;
    }

    public BuildingTypeSettings? FindBuildingType(string id) =>
        BuildingTypes.FirstOrDefault(t => t.Id == id);

    private void Validate()
    {
        if (Map.Width <= 0 || Map.Height <= 0)
            throw new InvalidDataException("map.width и map.height должны быть больше 0.");
        if (TicksPerSecond <= 0)
            throw new InvalidDataException("ticksPerSecond должен быть больше 0.");
        if (Resident.TicksPerCell <= 0)
            throw new InvalidDataException("resident.ticksPerCell должен быть больше 0.");
        if (BuildingTypes.Count == 0)
            throw new InvalidDataException("buildingTypes не должен быть пустым.");
        if (BuildingTypes.Select(t => t.Id).Distinct().Count() != BuildingTypes.Count)
            throw new InvalidDataException("В buildingTypes есть повторяющиеся id.");
    }
}