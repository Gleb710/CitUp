namespace CitUp.Sim;

public static class GameData
{
    /// <summary>Читает JSON, встроенный в сборку Sim (см. EmbeddedResource в Sim.csproj).</summary>
    public static string LoadText(string fileName)
    {
        var resourceName = $"Data.{fileName}";
        using var stream = typeof(GameData).Assembly.GetManifestResourceStream(resourceName)
                           ?? throw new FileNotFoundException(
                               $"Встроенный файл данных не найден: {resourceName}. Проверьте EmbeddedResource в Sim.csproj.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}