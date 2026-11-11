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
    
    /// <summary>Поворот маски по часовой стрелке на 0, 90, 180 или 270 градусов.</summary>
    public BuildingMask Rotate(int degrees)
    {
        if (degrees is not (0 or 90 or 180 or 270))
            throw new ArgumentOutOfRangeException(
                nameof(degrees),
                degrees,
                "Допустимы повороты 0, 90, 180 и 270 градусов.");

        if (degrees == 0)
            return this;

        var width = Width;
        var height = Height;
        var newWidth = degrees == 180 ? width : height;
        var newHeight = degrees == 180 ? height : width;

        var grid = new char[newHeight][];
        for (var y = 0; y < newHeight; y++)
        {
            grid[y] = new char[newWidth];
            Array.Fill(grid[y], '.');
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < Rows[y].Length; x++)
            {
                var (newX, newY) = degrees switch
                {
                    90 => (height - 1 - y, x),
                    180 => (width - 1 - x, height - 1 - y),
                    _ => (y, width - 1 - x)
                };

                grid[newY][newX] = Rows[y][x];
            }
        }

        return new BuildingMask(grid.Select(row => new string(row)).ToArray());
    }
}