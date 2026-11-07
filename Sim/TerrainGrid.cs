namespace CitUp.Sim;

/// <summary>Прямоугольная сетка типов местности.</summary>
public sealed class TerrainGrid
{
    private readonly TerrainType[] _cells;

    public int Width { get; }

    public int Height { get; }

    public TerrainGrid(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(width),
                width,
                "Ширина карты должна быть больше 0.");

        if (height <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(height),
                height,
                "Высота карты должна быть больше 0.");

        Width = width;
        Height = height;
        _cells = new TerrainType[width * height];
    }

    public bool IsInside(Cell cell) =>
        cell.X >= 0 &&
        cell.Y >= 0 &&
        cell.X < Width &&
        cell.Y < Height;

    public TerrainType GetTerrain(Cell cell)
    {
        ThrowIfOutside(cell);
        return _cells[GetIndex(cell)];
    }

    public void SetTerrain(Cell cell, TerrainType terrain)
    {
        ThrowIfOutside(cell);
        _cells[GetIndex(cell)] = terrain;
    }

    private int GetIndex(Cell cell) => cell.Y * Width + cell.X;

    private void ThrowIfOutside(Cell cell)
    {
        if (!IsInside(cell))
            throw new ArgumentOutOfRangeException(
                nameof(cell),
                cell,
                $"Клетка {cell} находится вне карты {Width}×{Height}.");
    }
}