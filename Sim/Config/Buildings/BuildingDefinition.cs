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
    int ConstructionTimeTicks)
{
    public int Width => Footprint.Width;

    public int Height => Footprint.Height;
    
    /// <summary>Разрешён ли поворот. 0° доступен любому зданию.</summary>
    public bool SupportsRotation(int rotation) =>
        rotation == 0 || Rotations.Contains(rotation);
}