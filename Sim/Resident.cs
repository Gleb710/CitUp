namespace CitUp.Sim;

/// <summary>Житель. Состояние меняется только через World.Apply.</summary>
public sealed class Resident
{
    public int Id { get; }

    /// <summary>Где стоит. Пока житель идёт, здесь стартовая клетка маршрута.</summary>
    public Cell Cell { get; internal set; }

    /// <summary>Текущий маршрут (включая старт и финиш) или null, если житель стоит.</summary>
    public IReadOnlyList<Cell>? Path { get; internal set; }

    public long WalkStartTick { get; internal set; }

    public bool IsWalking => Path is not null;

    public Resident(int id, Cell cell)
    {
        Id = id;
        Cell = cell;
    }

    /// <summary>Тик, на котором житель окажется в последней клетке маршрута.</summary>
    public long WalkEndTick(int ticksPerCell)
    {
        if (Path is null)
            throw new InvalidOperationException("Житель не идёт, у прогулки нет конца.");
        return WalkStartTick + (long)(Path.Count - 1) * ticksPerCell;
    }

    /// <summary>В какой клетке житель на указанном тике. До старта это первая клетка, после финиша последняя.</summary>
    public Cell CellAt(long tick, int ticksPerCell)
    {
        if (Path is null)
            return Cell;

        long steps = (tick - WalkStartTick) / ticksPerCell;
        if (tick < WalkStartTick) steps = 0;
        if (steps > Path.Count - 1) steps = Path.Count - 1;
        return Path[(int)steps];
    }

    /// <summary>Мешает ли житель строить в этой клетке: стоит в ней или маршрут проходит через неё.</summary>
    public bool Blocks(Cell cell) => Path is null ? Cell == cell : Path.Contains(cell);
}