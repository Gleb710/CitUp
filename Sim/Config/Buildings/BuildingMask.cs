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
    
    /// <summary>
    /// Возвращает мировые клетки указанного типа.
    /// origin — мировая клетка, соответствующая позиции (0, 0) этой маски.
    /// Не обрезает результат по границам карты.
    /// </summary>
    public IReadOnlyList<Cell> GetWorldCells(
        Cell origin,
        BuildingMaskCell cellType)
    {
        if (!Enum.IsDefined(typeof(BuildingMaskCell), cellType) ||
            cellType == BuildingMaskCell.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cellType),
                cellType,
                "Нужно указать непустой тип клетки маски.");
        }

        var cells = new List<Cell>();

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                if (GetCell(x, y) != cellType)
                    continue;

                cells.Add(new Cell(
                    checked(origin.X + x),
                    checked(origin.Y + y)));
            }
        }

        return cells.AsReadOnly();
    }
}