namespace CitUp.Sim;

/// <summary>Прямоугольная сетка типов местности.</summary>
public sealed class TerrainGrid
{
    private readonly TerrainType[] _cells;
    private readonly int[] _elevations;
    
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
        var cellCount = width * height;
        _cells = new TerrainType[cellCount];
        _elevations = new int[cellCount];
        
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
    
    public int GetElevation(Cell cell)
    {
        ThrowIfOutside(cell);
        return _elevations[GetIndex(cell)];
    }

    public void SetElevation(Cell cell, int elevation)
    {
        ThrowIfOutside(cell);

        if (elevation < 0)
            throw new ArgumentOutOfRangeException(
                nameof(elevation),
                elevation,
                "Высота клетки не может быть отрицательной.");

        _elevations[GetIndex(cell)] = elevation;
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