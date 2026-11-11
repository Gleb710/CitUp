namespace CitUp.Sim;

/// <summary>
/// Описание здания из Data/buildings.json:
/// геометрия, стоимость и время строительства.
/// Рецепты, профессии и требования к местности будут добавлены позже.
/// </summary>
public sealed record BuildingDefinition(
    string Id,
    string Name,
    BuildingMask Footprint,
    BuildingMask Clearance,
    BuildingMask? Access,
    IReadOnlyList<int> Rotations,
    BuildingCost Cost,
    int ConstructionTimeTicks,
    string Color = "#ffffff",
    bool Buildable = true)
{
    public int Width => Footprint.Width;

    public int Height => Footprint.Height;
    
    /// <summary>Разрешён ли поворот. 0° доступен любому зданию.</summary>
    public bool SupportsRotation(int rotation) =>
        rotation == 0 || Rotations.Contains(rotation);
    
    /// <summary>Есть ли у здания хоть один поворот, кроме 0°.</summary>
    public bool CanRotate => Rotations.Any(rotation => rotation != 0);

    /// <summary>Следующий разрешённый поворот по кругу: 0, 90, 180, 270. Для зданий без поворотов всегда 0.</summary>
    public int NextRotation(int current)
    {
        for (var step = 1; step <= 4; step++)
        {
            var candidate = (current + 90 * step) % 360;

            if (SupportsRotation(candidate))
                return candidate;
        }

        return 0;
    }
}