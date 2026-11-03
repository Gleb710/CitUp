namespace CitUp.Sim;

/// <summary>Поиск пути по клеткам в ширину (BFS). Ходить можно в 4 стороны, здания непроходимы.</summary>
public static class GridPathfinder
{
    // Порядок фиксирован: тогда при равных вариантах всегда выбирается один и тот же путь.
    internal static readonly Cell[] Directions = { new(1, 0), new(0, 1), new(-1, 0), new(0, -1) };

    public static bool IsWalkable(World world, Cell cell) =>
        world.IsInside(cell) && world.BuildingAt(cell) is null;

    /// <summary>Кратчайший путь от from до to (обе клетки входят в результат) или null, если пути нет.</summary>
    public static IReadOnlyList<Cell>? FindPath(World world, Cell from, Cell to)
    {
        if (!IsWalkable(world, from) || !IsWalkable(world, to))
            return null;
        if (from == to)
            return new[] { from };

        // cameFrom[клетка] = откуда мы в неё пришли; заодно служит списком посещённых
        var cameFrom = new Dictionary<Cell, Cell> { [from] = from };
        var queue = new Queue<Cell>();
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var d in Directions)
            {
                var next = new Cell(current.X + d.X, current.Y + d.Y);
                if (cameFrom.ContainsKey(next) || !IsWalkable(world, next))
                    continue;

                cameFrom[next] = current;
                if (next == to)
                    return Reconstruct(cameFrom, from, to);
                queue.Enqueue(next);
            }
        }

        return null;
    }

    private static List<Cell> Reconstruct(Dictionary<Cell, Cell> cameFrom, Cell from, Cell to)
    {
        var path = new List<Cell> { to };
        var cell = to;
        while (cell != from)
        {
            cell = cameFrom[cell];
            path.Add(cell);
        }
        path.Reverse();
        return path;
    }
}