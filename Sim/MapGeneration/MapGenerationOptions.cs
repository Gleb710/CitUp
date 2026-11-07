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
    int MaxLakeSize)
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
    }
}