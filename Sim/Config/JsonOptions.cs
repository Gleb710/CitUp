using System.Text.Json;

namespace CitUp.Sim;

/// <summary>Общие настройки чтения JSON для всех игровых конфигураций.</summary>
public static class JsonOptions
{
    public static readonly JsonSerializerOptions Shared = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}