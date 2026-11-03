namespace CitUp.Sim;

/// <summary>Всё состояние игрового мира. Позже сюда добавятся здания и жители.</summary>
public sealed class World
{
    public WorldConfig Config { get; }
    public int Width => Config.Map.Width;
    public int Height => Config.Map.Height;

    /// <summary>Номер текущего тика. Время в игре измеряется тиками, а не секундами и не кадрами.</summary>
    public long CurrentTick { get; private set; }

    public World(WorldConfig config)
    {
        Config = config;
    }

    public bool IsInside(Cell cell) =>
        cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

    // internal: двигать время может только SimEngine из этой же сборки
    internal void AdvanceTick() => CurrentTick++;
}