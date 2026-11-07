namespace CitUp.Sim;

/// <summary>
/// Параметры генерации природной карты.
/// Seed задаётся при создании партии и определяет конкретную карту.
/// </summary>
public sealed record MapGenerationOptions(
    int Width,
    int Height,
    ulong Seed,
    int LakeCount,
    int MinLakeSize,
    int MaxLakeSize,
    int CliffClusterCount = 4,
    int MinCliffSize = 6,
    int MaxCliffSize = 16,
    int ResourceVeinCount = 3,
    int ForestClusterCount = 12,
    double SlopeChance = 0.35)
{
    public void Validate()
    {
        if (Width <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(Width),
                Width,
                "Ширина карты должна быть больше 0.");

        if (Height <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(Height),
                Height,
                "Высота карты должна быть больше 0.");

        if (LakeCount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(LakeCount),
                LakeCount,
                "Количество озёр не может быть отрицательным.");

        if (MinLakeSize <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(MinLakeSize),
                MinLakeSize,
                "Минимальный размер озера должен быть больше 0.");

        if (MaxLakeSize < MinLakeSize)
            throw new ArgumentOutOfRangeException(
                nameof(MaxLakeSize),
                MaxLakeSize,
                "Максимальный размер озера не может быть меньше минимального.");

        if (CliffClusterCount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(CliffClusterCount),
                CliffClusterCount,
                "Количество обрывов не может быть отрицательным.");

        if (MinCliffSize <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(MinCliffSize),
                MinCliffSize,
                "Минимальный размер обрыва должен быть больше 0.");

        if (MaxCliffSize < MinCliffSize)
            throw new ArgumentOutOfRangeException(
                nameof(MaxCliffSize),
                MaxCliffSize,
                "Максимальный размер обрыва не может быть меньше минимального.");

        if (ResourceVeinCount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(ResourceVeinCount),
                ResourceVeinCount,
                "Количество ресурсных жил не может быть отрицательным.");

        if (ForestClusterCount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(ForestClusterCount),
                ForestClusterCount,
                "Количество лесных кластеров не может быть отрицательным.");
        
        if (SlopeChance is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(
                nameof(SlopeChance),
                SlopeChance,
                "Вероятность склона должна быть от 0 до 1.");
    }
}