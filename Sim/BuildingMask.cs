namespace CitUp.Sim;

/// <summary>
/// Прямоугольная маска здания: footprint, clearance или access.
/// Каждая строка — один ряд маски.
/// </summary>
public sealed record BuildingMask(IReadOnlyList<string> Rows)
{
    public int Width => Rows.Count == 0 ? 0 : Rows[0].Length;

    public int Height => Rows.Count;

    public BuildingMaskCell GetCell(int x, int y)
    {
        if (y < 0 || y >= Height)
            return BuildingMaskCell.None;

        var row = Rows[y];

        if (x < 0 || x >= row.Length)
            return BuildingMaskCell.None;

        return row[x] switch
        {
            'F' => BuildingMaskCell.Footprint,
            'P' => BuildingMaskCell.PassableFootprint,
            'C' => BuildingMaskCell.Clearance,
            'A' => BuildingMaskCell.Access,
            _ => BuildingMaskCell.None
        };
    }
}