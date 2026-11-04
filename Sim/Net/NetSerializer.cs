using System.Text.Json;
using System.Text.Json.Serialization;

namespace CitUp.Sim;

/// <summary>Переводит сообщения в байты (UTF-8 JSON) и обратно.</summary>
public static class NetSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        // Перечисления пишем словами ("CellOccupied"), так журнал читается без таблицы кодов
        Converters = { new JsonStringEnumConverter() }
    };

    public static byte[] Serialize(NetMessage message) =>
        JsonSerializer.SerializeToUtf8Bytes<NetMessage>(message, Options);

    /// <summary>Разбирает байты в сообщение. Неверные данные дают InvalidDataException с понятным текстом.</summary>
    public static NetMessage Deserialize(byte[] data)
    {
        try
        {
            return JsonSerializer.Deserialize<NetMessage>(data, Options)
                   ?? throw new InvalidDataException("Пришло пустое сообщение (null).");
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new InvalidDataException($"Не удалось разобрать сообщение: {ex.Message}", ex);
        }
    }

    /// <summary>JSON-текст сообщения: для журнала и для тестов.</summary>
    public static string ToJson(NetMessage message) =>
        JsonSerializer.Serialize<NetMessage>(message, Options);
}