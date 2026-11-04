using CitUp.Sim;
using Godot;

namespace CitUp.Game;

/// <summary>Математика изометрии: клетка карты ↔ точка на экране. Ромб клетки 64×32 пикселя.</summary>
public static class IsoGrid
{
    public const float TileWidth = 64f;
    public const float TileHeight = 32f;

    private const float HalfW = TileWidth / 2f;
    private const float HalfH = TileHeight / 2f;

    /// <summary>Верхняя вершина ромба клетки.</summary>
    public static Vector2 CellTop(Cell cell) =>
        new((cell.X - cell.Y) * HalfW, (cell.X + cell.Y) * HalfH);

    /// <summary>Центр ромба клетки.</summary>
    public static Vector2 CellCenter(Cell cell) => CellTop(cell) + new Vector2(0f, HalfH);

    /// <summary>Четыре вершины ромба по часовой стрелке: верх, право, низ, лево.</summary>
    public static Vector2[] CellDiamond(Cell cell)
    {
        var top = CellTop(cell);
        return new[]
        {
            top,
            top + new Vector2(HalfW, HalfH),
            top + new Vector2(0f, TileHeight),
            top + new Vector2(-HalfW, HalfH)
        };
    }

    /// <summary>Какой клетке принадлежит точка. Клетка может оказаться вне карты, проверять через World.IsInside.</summary>
    public static Cell PointToCell(Vector2 point)
    {
        float a = point.Y / HalfH;
        float b = point.X / HalfW;
        return new Cell(Mathf.FloorToInt((a + b) / 2f), Mathf.FloorToInt((a - b) / 2f));
    }

    /// <summary>Точка, на которую смотрит камера в начале игры: середина карты.</summary>
    public static Vector2 MapCenter(World world) =>
        CellTop(new Cell(world.Width / 2, world.Height / 2));
}