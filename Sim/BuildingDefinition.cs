namespace CitUp.Sim;

/// <summary>
/// Геометрия здания из Data/buildings.json.
/// Стоимость, рецепты, требования к местности и правила поворота будут добавлены позже.
/// </summary>
public sealed record BuildingDefinition(
    string Id,
    string Name,
    BuildingMask Footprint,
    BuildingMask Clearance,
    BuildingMask? Access,
    IReadOnlyList<int> Rotations)
{
    public int Width => Footprint.Width;

    public int Height => Footprint.Height;
}