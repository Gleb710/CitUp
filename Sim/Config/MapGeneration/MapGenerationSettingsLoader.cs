using System.Text.Json;

namespace CitUp.Sim;

/// <summary>Загружает и проверяет Data/map_generation.json.</summary>
public static class MapGenerationSettingsLoader
{
    public static MapGenerationSettings LoadDefault() =>
        FromJson(GameData.LoadText("map_generation.json"));

    public static MapGenerationSettings FromJson(string json)
    {
        MapGenerationSettingsData? data;

        try
        {
            data = JsonSerializer.Deserialize<MapGenerationSettingsData>(
                json,
                JsonOptions.Shared);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Ошибка в Data/map_generation.json: {exception.Message}", exception);
        }

        if (data is null)
            throw new InvalidDataException("Data/map_generation.json пуст.");

        Validate(data);

        var map = data.Map
                  ?? throw new InvalidDataException(
                      "В Data/map_generation.json отсутствует map.");

        return new MapGenerationSettings(
            map.Width,
            map.Height,
            data.LakeCount,
            data.MinLakeSize,
            data.MaxLakeSize,
            data.CliffClusterCount,
            data.MinCliffSize,
            data.MaxCliffSize,
            data.ResourceVeinCount,
            data.ForestClusterCount,
            data.SlopeChance);
    }

    private static void Validate(MapGenerationSettingsData data)
    {
        if (data.Map is null)
            throw new InvalidDataException(
                "В Data/map_generation.json отсутствует map.");

        if (data.Map.Width <= 0 || data.Map.Height <= 0)
            throw new InvalidDataException(
                "В Data/map_generation.json map.width и map.height должны быть больше 0.");

        if (data.LakeCount < 0)
            throw new InvalidDataException(
                "В Data/map_generation.json lakeCount не может быть отрицательным.");

        if (data.MinLakeSize <= 0)
            throw new InvalidDataException(
                "В Data/map_generation.json minLakeSize должен быть больше 0.");

        if (data.MaxLakeSize < data.MinLakeSize)
            throw new InvalidDataException(
                "В Data/map_generation.json maxLakeSize не может быть меньше minLakeSize.");

        if (data.CliffClusterCount < 0)
            throw new InvalidDataException(
                "В Data/map_generation.json cliffClusterCount не может быть отрицательным.");

        if (data.MinCliffSize <= 0)
            throw new InvalidDataException(
                "В Data/map_generation.json minCliffSize должен быть больше 0.");

        if (data.MaxCliffSize < data.MinCliffSize)
            throw new InvalidDataException(
                "В Data/map_generation.json maxCliffSize не может быть меньше minCliffSize.");

        if (data.ResourceVeinCount < 0)
            throw new InvalidDataException(
                "В Data/map_generation.json resourceVeinCount не может быть отрицательным.");

        if (data.ForestClusterCount < 0)
            throw new InvalidDataException(
                "В Data/map_generation.json forestClusterCount не может быть отрицательным.");

        if (data.SlopeChance is < 0.0 or > 1.0)
            throw new InvalidDataException(
                "В Data/map_generation.json slopeChance должен быть от 0 до 1.");
    }

    private sealed class MapGenerationSettingsData
    {
        public MapSettingsData? Map { get; set; }

        public int LakeCount { get; set; }

        public int MinLakeSize { get; set; }

        public int MaxLakeSize { get; set; }

        public int CliffClusterCount { get; set; }

        public int MinCliffSize { get; set; }

        public int MaxCliffSize { get; set; }

        public int ResourceVeinCount { get; set; }

        public int ForestClusterCount { get; set; }

        public double SlopeChance { get; set; }
    }

    private sealed class MapSettingsData
    {
        public int Width { get; set; }

        public int Height { get; set; }
    }
}