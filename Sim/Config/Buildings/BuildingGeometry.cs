namespace CitUp.Sim;

/// <summary>
/// Геометрия здания без поворота.
/// Origin соответствует позиции (0, 0) маски Footprint.
/// Остальные маски совмещаются с Footprint по центру.
/// </summary>
public static class BuildingGeometry
{
    public static IReadOnlyList<Cell> GetBlockedCells(
        BuildingDefinition building,
        Cell origin)
    {
        ArgumentNullException.ThrowIfNull(building);

        return building.Footprint.GetWorldCells(
            origin,
            BuildingMaskCell.Footprint);
    }

    public static IReadOnlyList<Cell> GetPassableCells(
        BuildingDefinition building,
        Cell origin)
    {
        ArgumentNullException.ThrowIfNull(building);

        return building.Footprint.GetWorldCells(
            origin,
            BuildingMaskCell.PassableFootprint);
    }

    public static IReadOnlyList<Cell> GetFootprintCells(
        BuildingDefinition building,
        Cell origin)
    {
        ArgumentNullException.ThrowIfNull(building);

        return GetBlockedCells(building, origin)
            .Concat(GetPassableCells(building, origin))
            .ToArray();
    }

    public static IReadOnlyList<Cell> GetClearanceCells(
        BuildingDefinition building,
        Cell origin)
    {
        ArgumentNullException.ThrowIfNull(building);

        var maskOrigin = GetAlignedOrigin(
            building.Footprint,
            building.Clearance,
            origin);

        return building.Clearance.GetWorldCells(
            maskOrigin,
            BuildingMaskCell.Clearance);
    }

    public static IReadOnlyList<Cell> GetAccessCells(
        BuildingDefinition building,
        Cell origin)
    {
        ArgumentNullException.ThrowIfNull(building);

        if (building.Access is null)
            return Array.Empty<Cell>();

        var maskOrigin = GetAlignedOrigin(
            building.Footprint,
            building.Access,
            origin);

        return building.Access.GetWorldCells(
            maskOrigin,
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