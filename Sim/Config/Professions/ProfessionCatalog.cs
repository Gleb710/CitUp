using System.Text.Json;

namespace CitUp.Sim;

/// <summary>Каталог профессий, загруженный из Data/professions.json.</summary>
public sealed class ProfessionCatalog
{
    private readonly Dictionary<string, ProfessionDefinition> _professionsById;

    public IReadOnlyList<ProfessionDefinition> Professions { get; }

    public ProfessionCatalog(IReadOnlyList<ProfessionDefinition> professions)
    {
        Professions = professions;
        _professionsById = professions.ToDictionary(profession => profession.Id);
    }

    public ProfessionDefinition? FindProfession(string professionId) =>
        _professionsById.GetValueOrDefault(professionId);

    public static ProfessionCatalog LoadDefault() =>
        FromJson(GameData.LoadText("professions.json"));

    public static ProfessionCatalog FromJson(string json)
    {
        ProfessionCatalogData? data;

        try
        {
            data = JsonSerializer.Deserialize<ProfessionCatalogData>(json, JsonOptions.Shared);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Ошибка в Data/professions.json: {exception.Message}", exception);
        }

        if (data is null)
            throw new InvalidDataException("Data/professions.json пуст.");

        Validate(data);

        var professions = data.Professions
            .Select(profession => new ProfessionDefinition(
                profession.Id,
                profession.Name,
                profession.WorkTicks,
                profession.RestTicks))
            .ToArray();

        return new ProfessionCatalog(professions);
    }

    private static void Validate(ProfessionCatalogData data)
    {
        if (data.Professions is null || data.Professions.Count == 0)
            throw new InvalidDataException(
                "В Data/professions.json professions не должен быть пустым.");

        var duplicateId = data.Professions
            .GroupBy(profession => profession.Id)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateId is not null)
            throw new InvalidDataException(
                $"В Data/professions.json повторяется id профессии: {duplicateId.Key}.");

        foreach (var profession in data.Professions)
        {
            if (string.IsNullOrWhiteSpace(profession.Id))
                throw new InvalidDataException(
                    "В Data/professions.json у одной из профессий отсутствует id.");

            if (string.IsNullOrWhiteSpace(profession.Name))
                throw new InvalidDataException(
                    $"В Data/professions.json у профессии {profession.Id} отсутствует name.");

            if (profession.WorkTicks <= 0)
                throw new InvalidDataException(
                    $"В Data/professions.json у профессии {profession.Id} " +
                    "workTicks должен быть больше 0.");

            if (profession.RestTicks < 0)
                throw new InvalidDataException(
                    $"В Data/professions.json у профессии {profession.Id} " +
                    "restTicks не может быть отрицательным.");
        }
    }

    private sealed class ProfessionCatalogData
    {
        public List<ProfessionData> Professions { get; set; } = new();
    }

    private sealed class ProfessionData
    {
        public string Id { get; set; } = "";

        public string Name { get; set; } = "";

        public int WorkTicks { get; set; }

        public int RestTicks { get; set; }
    }
}