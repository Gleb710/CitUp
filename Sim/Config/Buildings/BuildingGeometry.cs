namespace CitUp.Sim;

/// <summary>
/// Геометрия здания. Origin соответствует позиции (0, 0) повёрнутой маски Footprint.
/// Остальные маски поворачиваются так же и совмещаются с Footprint по центру.
/// </summary>
public static class BuildingGeometry
{
    public static IReadOnlyList<Cell> GetBlockedCells(
        BuildingDefinition building,
        Cell origin,
        int rotation = 0)
    {
        ArgumentNullException.ThrowIfNull(building);

        return building.Footprint.Rotate(rotation).GetWorldCells(
            origin,
            BuildingMaskCell.Footprint);
    }

    public static IReadOnlyList<Cell> GetPassableCells(
        BuildingDefinition building,
        Cell origin,
        int rotation = 0)
    {
        ArgumentNullException.ThrowIfNull(building);

        return building.Footprint.Rotate(rotation).GetWorldCells(
            origin,
            BuildingMaskCell.PassableFootprint);
    }

    public static IReadOnlyList<Cell> GetFootprintCells(
        BuildingDefinition building,
        Cell origin,
        int rotation = 0)
    {
        ArgumentNullException.ThrowIfNull(building);

        return GetBlockedCells(building, origin, rotation)
            .Concat(GetPassableCells(building, origin, rotation))
            .ToArray();
    }

    public static IReadOnlyList<Cell> GetClearanceCells(
        BuildingDefinition building,
        Cell origin,
        int rotation = 0)
    {
        ArgumentNullException.ThrowIfNull(building);

        var footprint = building.Footprint.Rotate(rotation);
        var clearance = building.Clearance.Rotate(rotation);

        return clearance.GetWorldCells(
            GetAlignedOrigin(footprint, clearance, origin),
            BuildingMaskCell.Clearance);
    }

    public static IReadOnlyList<Cell> GetAccessCells(
        BuildingDefinition building,
        Cell origin,
        int rotation = 0)
    {
        ArgumentNullException.ThrowIfNull(building);

        if (building.Access is null)
            return Array.Empty<Cell>();

        var footprint = building.Footprint.Rotate(rotation);
        var access = building.Access.Rotate(rotation);

        return access.GetWorldCells(
            GetAlignedOrigin(footprint, access, origin),
            BuildingMaskCell.Access);
    }

    private static Cell GetAlignedOrigin(
        BuildingMask footprint,
        BuildingMask mask,
        Cell origin)
    {
        var widthDifference = footprint.Width - mask.Width;
        var heightDifference = footprint.Height - mask.Height;

        if (widthDifference % 2 != 0 ||
            heightDifference % 2 != 0)
        {
            throw new InvalidOperationException(
                $"Нельзя совместить по центру маски " +
                $"{footprint.Width}×{footprint.Height} и " +
                $"{mask.Width}×{mask.Height}: " +
                "разница размеров должна быть чётной.");
        }

        return new Cell(
            checked(origin.X + widthDifference / 2),
            checked(origin.Y + heightDifference / 2));
    }
}