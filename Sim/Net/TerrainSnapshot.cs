namespace CitUp.Sim;

/// <summary>Карта местности в снимке: тип и высота каждой клетки, строка за строкой.</summary>
public sealed record TerrainSnapshot(int Width, int Height, int[] Cells, int[] Elevations)
{
    private const int MaxSide = 1024;

    public static TerrainSnapshot From(TerrainGrid terrain)
    {
        var cells = new int[terrain.Width * terrain.Height];
        var elevations = new int[cells.Length];

        for (var y = 0; y < terrain.Height; y++)
        {
            for (var x = 0; x < terrain.Width; x++)
            {
                var cell = new Cell(x, y);
                var index = y * terrain.Width + x;

                cells[index] = (int)terrain.GetTerrain(cell);
                elevations[index] = terrain.GetElevation(cell);
            }
        }

        return new TerrainSnapshot(terrain.Width, terrain.Height, cells, elevations);
    }

    /// <summary>Строит сетку из снимка. Из сети может прийти что угодно, поэтому всё проверяется.</summary>
    public TerrainGrid ToGrid()
    {
        if (Width is <= 0 or > MaxSide || Height is <= 0 or > MaxSide)
            throw new InvalidDataException($"Снимок: недопустимый размер карты {Width}×{Height}.");

        var count = Width * Height;

        if (Cells is null || Elevations is null || Cells.Length != count || Elevations.Length != count)
            throw new InvalidDataException(
                $"Снимок: данные карты не соответствуют размеру {Width}×{Height}.");

        var grid = new TerrainGrid(Width, Height);

        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var index = y * Width + x;
                var terrain = (TerrainType)Cells[index];

                if (!Enum.IsDefined(terrain))
                    throw new InvalidDataException(
                        $"Снимок: неизвестный тип местности {Cells[index]} в клетке ({x}, {y}).");

                if (Elevations[index] < 0)
                    throw new InvalidDataException(
                        $"Снимок: отрицательная высота в клетке ({x}, {y}).");

                var cell = new Cell(x, y);
                grid.SetTerrain(cell, terrain);
                grid.SetElevation(cell, Elevations[index]);
            }
        }

        return grid;
    }
}