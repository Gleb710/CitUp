namespace CitUp.Sim;

/// <summary>
/// Настройки генерации природной карты из Data/map_generation.json.
/// Seed задаётся отдельно при создании партии.
/// </summary>
public sealed record MapGenerationSettings(
    int Width,
    int Height,
    int LakeCount,
    int MinLakeSize,
    int MaxLakeSize,
    int CliffClusterCount,
    int MinCliffSize,
    int MaxCliffSize,
    int ResourceVeinCount,
    int ForestClusterCount,
    double SlopeChance)
{
    public MapGenerationOptions ToOptions(ulong seed) => new(
        Width,
        Height,
        seed,
        LakeCount,
        MinLakeSize,
        MaxLakeSize,
        CliffClusterCount,
        MinCliffSize,
        MaxCliffSize,
        ResourceVeinCount,
        ForestClusterCount,
        SlopeChance);
}